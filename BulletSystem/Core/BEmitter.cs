using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PixelDust.Audiophile;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public enum ETempPerspective
{
    eTopDown,
    eSideOn,
    eOnRails
}


namespace NiBullets
{
    
    public enum EBulletFireType //?
    {
        eBasic, //if basic, skip any curve checks in update to save processing. 
        ePureAnimationCurve, //will probably NEVER be used 
        eMathFunction
    }

    public enum EFireLimitType
    {
        eBulletCount,
        eTimer
    }

    public enum idkType //?
    {
        eBasic,
        eAnimationCurve,

    }
    
    public enum EVolleyAngleIncrementType //?
    {
        eExactDegreeValues,
        eIncrementByDegrees,

    }

    public class BEmitter : MonoBehaviour, IEnemyFireable
    {
        private bool emitterIsOn;
        [SerializeField] private bool emitterOnByDefault = false;
        
        [SerializeField] private Bullet bulletObject;   //CHANGE THIS TO TAKE THE BULLET COMPONENT INSTEAD SO RETRIEVING THE ID IS CHEAPER
        public ETempPerspective tempPerspective;

        public EBulletFireType typeOfBulletBulletFire; //based on this type, skip certain update checks to save on processing
        
        [Tooltip("Negates the velocity of the scroller. Useful when firing directly at player")]
        public bool parentBulletToScroller = false;
        //Organisation of bullet parent hierarchy in inspector will get jumbled during gameplay but there is no practical way to avoid this -ak
        //Or not -ak
        private Transform scrollerTransform;
        
        
        [SerializeField] private float totalPhaseLifetime = 0; //should not be serialized?
        private float totalPhaseLifetime_WORLD = 0; //can't be modified by the emitter's speed manipulator

        [Tooltip("if true, first round is fired on activation before waiting intervals. Overrides start delay")]
        [SerializeField] private bool precookTimer = true;
        private float fireTimer = 0;
        [SerializeField] private float fireInterval = 0.2f;
        [SerializeField] private float startDelay = 0.0f;
        //For emitters that fire a single round only on command of the activate function, keep interval at 0
        
        [Tooltip("Nonstop fire without a bullet shot limit")]
        [SerializeField] private bool shouldRepeat = true;
        
        [Tooltip("number of bullets fired before current loop end")]
        [SerializeField] private int volleysFiredUntilEnd = 1;
        private int volleysFired_counter = 0;
        
        
        [SerializeField] private Vector3 baseVelocity = Vector3.forward;
        [SerializeField] private Vector3 baseAccel = Vector3.forward;

        [SerializeField]
        private float
            speedModifier =
                2; //for any force lost by overriding base velocity with a different direction, use speed modifier to make up for it

        //How much the bullet direction deviates from 
        [Range(0, 360)] [SerializeField] private float directionRandomSpread = 0; //in degrees

        //HASN'T BEEN MADE SO IT DOES THIS WITH REGULAR VELOCITY YET. CHANGE THIS.
        public bool makeAccelerationRelativeToNewDirection = true;

        //Uses player location as relative fire position. Default is -1 in the Z axis. Overrides base velocity
        [SerializeField] private bool fireTowardsPlayer;
        [SerializeField] private PlayerManager playerReference;

        [Range(1, 60)] [SerializeField] private int bulletsFiredAtATime = 1;

        //Components needed:
        //bullet properties varying throughout a single volley
        [SerializeField] private bool useCurveThroughoutVolley_angle;
        [SerializeField] private bool useCurveThroughoutVolley_angle_readCurveInDegrees;
        [SerializeField] private AnimationCurve currentVolleyModifier_angle;
        
        [SerializeField] [Tooltip("OVERRIDES CURVE. ARRAY MUST BE SAME LENGTH AS BULLETSFIREDATATIME")] private bool useArrayThroughoutVolley_angle; 
        [SerializeField]private float[] currentVolleyModifierArray_angle;
        [SerializeField] private EVolleyAngleIncrementType volleyAngleIncrementType;    //idfk how to describe this
        
        [SerializeField] private bool useCurveThroughoutVolley_velocity;
        [SerializeField] private AnimationCurve currentVolleyModifier_velocity;
        
        [SerializeField] private bool useCurveThroughoutVolley_accel;
        [SerializeField] private AnimationCurve currentVolleyModifier_accel;





        //TYPE: CURVE (things under here are only needed when the fire type enum is set to animation curve)

        public bool baseOffCurve_fireRate; //divides interval by curve value so bullets spawn more often
        [SerializeField] private AnimationCurve rate_fireRate;

        //This might be janky. Avoid using unless I know exactly what I'm doing for a bullet pattern. (CURRENTLY INCOMPLETE)
        public bool
            baseOffCurve_overallTimeScale; //increases speed of timer increment to make the whole system faster or slower

        [SerializeField]
        private AnimationCurve
            rate_overallTimeScale; //this curve is evaluated with the 'WORLD' lifetime instead of the regular one

        public bool
            baseOffCurve_fireDirection; //when used, 'base velocity' is overidden by fire direction but is still multiplied by speed modifier

        [SerializeField] private AnimationCurve rate_fireDirection;
        //should I change the curve to scale to 360 instead of just 0-1?



        public bool baseOffCurve_velocity; //when used, 'base velocity' is multiplied by curve value
        [SerializeField] private AnimationCurve rate_fireVelocity;

        public bool baseOffCurve_accel; //not complicated to activate it just applies after bullet is fired anyway
        [SerializeField] private AnimationCurve rate_fireAccel;

        public bool baseOffCurve_speedMultiplier;
        [SerializeField] private AnimationCurve rate_speedMultiplier;

        
        //How to make acceleration relative to initial direction?


        //TYPE: CURVE


        //Fire bullets that follow really specific curve
        [SerializeField] private bool baseBulletCoordOnPureCurve_x;
        [SerializeField] private bool baseBulletCoordOnPureCurve_y;
        [SerializeField] private AnimationCurve[] bulletCoordinateCurves_x;
        [SerializeField] private AnimationCurve[] bulletCoordinateCurves_y;
        [SerializeField] private float curveMagnifierValue;


        //make variable for this
        ////Options:
        /// Use curves but no velocity
        ///     skip all velocity/acceleration processing steps
        ///     if only one axis curve is intended to be used then there will be no movement on the other axis if velocity is 0
        /// Use curves in addition to velocity
        ///     velocity on all axes will need to be calculated
        ///     random rotation would be included in this category but initial rotation doesn't work with curves yet
        ///

        [Header("Sounds")]
        [SerializeField] private SoundEvent[] soundList;
        
        
        
        
        
        private void Awake()
        {
            emitterIsOn = emitterOnByDefault;
            if (precookTimer)
            {
                fireTimer = fireInterval;
            }
            else
            {
                fireTimer = -startDelay;
            }

        }

        // Start is called before the first frame update
        void Start()
        {
            playerReference = FindObjectOfType<PlayerManager>();
            scrollerTransform = GameObject.FindWithTag("ScrollerBulletHolder").transform;
            //playerReference = FindObjectOfType<TempPlayer>();
        }

        // Update is called once per frame
        protected virtual void Update()
        {

            //amin clean this function you bombaclaart ass bhenchod
            if (emitterIsOn)
            {



                #region Handle timer updating

                if (!baseOffCurve_overallTimeScale)
                {
                    totalPhaseLifetime_WORLD += Time.deltaTime; //not used yet //now used below

                    totalPhaseLifetime += Time.deltaTime;
                    fireTimer +=
                        Time.deltaTime; //do I want the fire timer to be affected by the timescale too or should just the other curve evaluations be affected
                    //fireTimer is already affected by rate curve
                }
                else
                {
                    //issue: need a seperate constant lifetime if lifetime is going to be modified by curve
                    float timeCurveEvaluation = rate_overallTimeScale.Evaluate(totalPhaseLifetime_WORLD);
                    totalPhaseLifetime += Time.deltaTime * timeCurveEvaluation;
                    fireTimer += Time.deltaTime * timeCurveEvaluation;
                    
                    totalPhaseLifetime_WORLD += Time.deltaTime;
                }

                #endregion

                #region Determine whether should fire

                bool shouldFire;
                if (baseOffCurve_fireRate)
                {
                    if (Mathf.Approximately(rate_fireRate.Evaluate(totalPhaseLifetime),
                        0)) //if curve is at 0 then don't do anything, this would be a 0 division
                    {
                        //While rate is at 0 should not fire as interval would be infinite
                        Debug.Log(
                            $"FIRE RATE IS 'APPROXIMATELY' ZERO. TRUE VALUE OF RATE IS {rate_fireRate.Evaluate(totalPhaseLifetime)}");
                        shouldFire = false;
                    }
                    else
                    {
                        shouldFire = fireTimer > fireInterval / rate_fireRate.Evaluate(totalPhaseLifetime);
                    }
                }
                else
                {
                    shouldFire = fireTimer > fireInterval;
                }

                #endregion

                if (shouldFire)
                {


                    //Fire
                    //Vector3 newVel = baseVelocity * rate_fireVelocity.Evaluate(totalPhaseLifetime);

                    //The axis to base rotations around based off current perspective
                    Vector3 rotationAxisVector = Vector3.up;

                    //No longer temp perspective, this is definitely used
                    switch (tempPerspective)
                    {
                        case ETempPerspective.eTopDown:
                            rotationAxisVector = Vector3.up;
                            break;

                        case ETempPerspective.eSideOn:
                            rotationAxisVector = Vector3.left;
                            break;

                        case ETempPerspective.eOnRails:
                            rotationAxisVector = Vector3.back; //idk
                            break;

                        default:
                            break;
                    }


                    Vector3 newVel = baseVelocity;
                    Vector3 newAccel = baseAccel;
                    float newSpeedModifier = speedModifier;

                    //Move this into for loop?
                    float angleVariation = Random.Range(-directionRandomSpread, directionRandomSpread); //Seed?


                    //Skip all these checks if fire type is set to simple

                    #region The processing of property rates

                    //Evaluate acceleration
                    if (baseOffCurve_accel)
                    {
                        newAccel = baseAccel * rate_fireAccel.Evaluate(totalPhaseLifetime);
                    }


                    if (!fireTowardsPlayer) //if not firing in direction of player (independent fire direction)
                    {

                        if (baseOffCurve_velocity)
                        {
                            newVel = newVel * rate_fireVelocity.Evaluate(totalPhaseLifetime);
                            newVel = Quaternion.AngleAxis(angleVariation, rotationAxisVector) * newVel;

                            if (makeAccelerationRelativeToNewDirection)
                                newAccel = Quaternion.AngleAxis(angleVariation, rotationAxisVector) * newAccel;

                        }

                        if (baseOffCurve_fireDirection) //OVERRIDES VELOCITY
                        {
                            newVel = Quaternion.AngleAxis(
                                rate_fireDirection.Evaluate(totalPhaseLifetime) * 360 + angleVariation,
                                rotationAxisVector) * Vector3.forward;

                            if (makeAccelerationRelativeToNewDirection)
                                newAccel = Quaternion.AngleAxis(
                                    rate_fireDirection.Evaluate(totalPhaseLifetime) * 360 + angleVariation,
                                    rotationAxisVector) * newAccel;

                        }


                    }
                    else //if firing towards player
                    {
                        newVel = Vector3.Normalize(playerReference.transform.position - transform.position);

                        if (baseOffCurve_fireDirection) //OVERRIDES VELOCITY
                        {
                            newVel = Quaternion.AngleAxis(
                                rate_fireDirection.Evaluate(totalPhaseLifetime) * 360 + angleVariation,
                                rotationAxisVector) * newVel;

                            if (makeAccelerationRelativeToNewDirection)
                                newAccel = Quaternion.AngleAxis(
                                    rate_fireDirection.Evaluate(totalPhaseLifetime) * 360 + angleVariation,
                                    rotationAxisVector) * newAccel;

                        }
                        else
                        {
                            newVel = Quaternion.AngleAxis(angleVariation, rotationAxisVector) * newVel;

                            if (makeAccelerationRelativeToNewDirection)
                                newAccel = Quaternion.AngleAxis(angleVariation, rotationAxisVector) * newAccel;

                        }
                    }


                    #endregion

                    if (baseOffCurve_speedMultiplier)
                        newSpeedModifier = speedModifier * rate_speedMultiplier.Evaluate(totalPhaseLifetime);



                    //float newDegrees = 360 * rate_fireDirection.Evaluate(totalPhaseLifetime);

                    //Debug.Log("Degrees:  " + newDegrees);
                    //Debug.Log("Firing at velocity: " + newVel);

                    //ignore loop if it's only one bullet?

                    Vector3 coreVelocity = newVel;
                    
                    for (int j = 0; j < bulletsFiredAtATime; j++)
                    {
                        //Resets velocity at the start of each loop
                        if(volleyAngleIncrementType == EVolleyAngleIncrementType.eExactDegreeValues) newVel = coreVelocity;
                        /*Debug.Log("J is " + j + ". Angle is " + currentVolleyModifier_angle.Evaluate(j) *
                            (useCurveThroughoutVolley_angle_readCurveInDegrees ? 1 : 360));
                        Debug.Log($"Evaluated curve value: {currentVolleyModifier_angle.Evaluate(j)}");*/
                        //modifications per increment:
                        //rotation
                        //velocity scale
                        //acceleration scale
                        Bullet newBullet = BulletPool.Instance.RetrieveBullet(bulletObject.publicBulletID);
                        newBullet.ClearTrailRenderer();
                        newBullet.transform.position = transform.position;

                        if (parentBulletToScroller)
                        {
                            if (scrollerTransform != null)
                            {
                                newBullet.transform.parent = scrollerTransform;
                            }
                            else
                            {
                                Debug.LogWarning("WARNING: could not find suitable SCROLLER parent object for bullet");
                            }
                        }
                        else
                        {
                            bulletObject.ReturnToDefaultParent();
                        }

                        //IMPORTANT BUG: ANGLEAXIS FUNCTION DOESN'T READ ANGLES BETWEEN 180-360 PROPERLY, NEEDS NEGATIVE 0-180 VALUES INSTEAD
                        if (useArrayThroughoutVolley_angle)             //ADD OPTION FOR RANDOM SPREAD AND RELATIVE TO VELOCITY/PLAYER DIRECTION LATER
                        {
                            int index = 0;
                            if (j > currentVolleyModifierArray_angle.Length)
                            {
                                //get factors bigger
                                int fac = j % currentVolleyModifierArray_angle.Length;
                                index = fac;
                            }
                            else
                            {
                                index = j;
                            }
                            
                           //Debug.Log($"DEGREES IS {currentVolleyModifierArray_angle[index]}, VELOCITY CHANGED FROM {newVel} TO {Quaternion.AngleAxis(currentVolleyModifierArray_angle[index], rotationAxisVector) * newVel}");
                            
                            newVel = Quaternion.AngleAxis(currentVolleyModifierArray_angle[index], rotationAxisVector) * newVel;
                        }
                        else if (useCurveThroughoutVolley_angle)
                        {
                            if (useCurveThroughoutVolley_angle_readCurveInDegrees)
                            {
                                newVel = Quaternion.AngleAxis(currentVolleyModifier_angle.Evaluate(j), rotationAxisVector) * newVel;
                            }
                            else
                            {
                                newVel = Quaternion.AngleAxis(currentVolleyModifier_angle.Evaluate(j) * (360), rotationAxisVector) * newVel;
                            }
                        }
                            
                        if (useCurveThroughoutVolley_velocity) newVel *= currentVolleyModifier_velocity.Evaluate(j);
                        if (useCurveThroughoutVolley_accel) newAccel *= currentVolleyModifier_accel.Evaluate(j);
                        //Debug.Log("Firing at velocity: " + newVel);


                        //DO DEFENSIVE CODE TO PREVENT ARRAY OUT OF BOUNDS LATER
                        if (baseBulletCoordOnPureCurve_x)
                        {
                            if (baseBulletCoordOnPureCurve_y) //X and Y
                            {
                                newBullet.OnCreateBullet(curveMagnifierValue, bulletCoordinateCurves_x[0],
                                    bulletCoordinateCurves_y[0], newVel * speedModifier, newAccel, tempPerspective);
                            }
                            else //Just X
                            {
                                newBullet.OnCreateBullet(curveMagnifierValue, bulletCoordinateCurves_x[0],
                                    newVel * speedModifier, newAccel, true, tempPerspective);
                            }
                        }
                        else if (baseBulletCoordOnPureCurve_y) //Just Y
                        {
                            newBullet.OnCreateBullet(curveMagnifierValue, bulletCoordinateCurves_y[0],
                                newVel * speedModifier, newAccel, true, tempPerspective);

                        }
                        else
                        {
                            newBullet.OnCreateBullet(newVel * speedModifier, newAccel, tempPerspective);
                        }



                    }

                    //Instantiate(bulletObject, transform.position, Quaternion.identity).GetComponent<Bullet>().OnCreateBullet(newVel * speedModifier, newAccel);


                    if (soundList.Length > 0) soundList[0].Play();

                    fireTimer = 0;

                    volleysFired_counter++;

                    if (volleysFired_counter >= volleysFiredUntilEnd)
                    {
                        if (!shouldRepeat) emitterIsOn = false;
                        volleysFired_counter = 0;
                    }
                }

            }
        }


        public virtual void ActivateEmitter()
        {
            fireTimer = 0;
            totalPhaseLifetime = 0;
            volleysFired_counter = 0;
            
            if (precookTimer)
            {
                fireTimer = fireInterval;
            }
            else
            {
                fireTimer = -startDelay;
            }
            
            emitterIsOn = true;
        }

        public virtual void StopEmitter()
        {
            emitterIsOn = false;
            
            fireTimer = 0;
            totalPhaseLifetime = 0;
            volleysFired_counter = 0;

            if (precookTimer) fireTimer = fireInterval;
        }

        public void PrimeRotationArray()
        {
            float h = (float)360 / (float)bulletsFiredAtATime;
            for (int i = 0; i < currentVolleyModifierArray_angle.Length; i++)
            {
                currentVolleyModifierArray_angle[i] = h * i;
            }
        }


        //public void Immediate

        private void OnDrawGizmos()
        {

            /*Vector3 rotationAxisVector = Vector3.up;
            Vector3 rotationAxisVectorIdkWhatThisOneIsFor = Vector3.up;
    
            switch (tempPerspective)
            {
                case ETempPerspective.eTopDown:
                    rotationAxisVector = Vector3.up;
                    rotationAxisVectorIdkWhatThisOneIsFor = Vector3.forward;
                    break;
                
                case ETempPerspective.eSideOn:
                    rotationAxisVector = Vector3.left;
                    rotationAxisVectorIdkWhatThisOneIsFor = Vector3.forward;
    
                    break;
                
                case ETempPerspective.eOnRails:
                    rotationAxisVector = Vector3.back;    //idk
                    rotationAxisVectorIdkWhatThisOneIsFor = Vector3.forward;    //???
    
                    break;
                
                default:
                    
                    break;
            }
            
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, Quaternion.AngleAxis(rate_fireDirection.Evaluate(totalPhaseLifetime) * 360, rotationAxisVector) * rotationAxisVectorIdkWhatThisOneIsFor);*/
        }




        #region Getters for editor

        //GETTERS FOR EDITOR

        public int Get_bulletsFiredAtATime()
        {
            return bulletsFiredAtATime;
        }

        public bool Get_baseOffCurve_fireRate()
        {
            return baseOffCurve_fireRate;
        }

        public bool Get_baseOffCurve_overallTimeScale()
        {
            return baseOffCurve_overallTimeScale;
        }

        public bool Get_baseOffCurve_fireDirection()
        {
            return baseOffCurve_fireDirection;
        }

        public bool Get_baseOffCurve_velocity()
        {
            return baseOffCurve_velocity;
        }

        public bool Get_baseOffCurve_accel()
        {
            return baseOffCurve_accel;
        }

        public bool Get_baseOffCurve_speedMultiplier()
        {
            return baseOffCurve_speedMultiplier;
        }

        public bool Get_baseBulletCoordOnPureCurve_x()
        {
            return baseBulletCoordOnPureCurve_x;
        }

        public bool Get_baseBulletCoordOnPureCurve_y()
        {
            return baseBulletCoordOnPureCurve_y;
        }

        public bool Get_baseBulletCoordOnPureCurve_useCurveThroughoutVolley_angle()
        {
            return useCurveThroughoutVolley_angle;
        }
        
        public bool Get_baseBulletCoordOnPureCurve_useARRAYThroughoutVolley_angle()
        {
            return useArrayThroughoutVolley_angle;
        }

        public bool Get_useCurveThroughoutVolley_velocity()
        {
            return useCurveThroughoutVolley_velocity;
        }

        public bool Get_useCurveThroughoutVolley_accel()
        {
            return useCurveThroughoutVolley_accel;
        }

        #endregion
        




    }

}


