using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace NiBullets
{


/////Fire types:
    /// Regular
    /// Base x and y off animation curve

    public enum ETypeOfBulletMovement
    {
        eRegular,
        eUseCurve
    }



    public class Bullet : MonoBehaviour
    {
        public int publicBulletID; //for identifying bullet in pool dictionary

        public ETempPerspective perspectiveMovement;
        public ETypeOfBulletMovement typeOfBulletMovement;

        private float lifetime;

        private Transform defaultParent;
        private Vector3 positionSpawned;
        public Vector3 GetOGPosition => positionSpawned;
        
        [SerializeField] private float maxDespawnDist_x = 70, maxDespawnDist_y = 40, maxDespawnDist_z = 200;  //z needs to be highest because this is the direction the scroller moves 

        [Header("Regular")] // -N
        //REGULAR
        //Not entirely necessary to be vector3s because all bullets will only move across 2 axis max
        public Vector3 position;
        public Vector3 velocity = new Vector3(0, 0, 1);

        public Vector3
            acceleration =
                new Vector3(0, 0, -0.1f); //should acceleration be applied with an additional force or proportionate?

        public Vector3 initialLaunchDirection = new Vector3(0, 0, -0.1f); //not used?

        public float speedModifier = 1;
        protected float speedModifier_default;

        public bool stopMovingAtZeroVelocity_x = true;
        public bool stopMovingAtZeroVelocity_y = true;
        public bool stopMovingAtZeroVelocity_z = true;

        [SerializeField] private float speedDecay = 0;

        //CURVE
        private float curveMagnitude = 0;
        private bool useCurve_x;
        private AnimationCurve curveMovement_x;
        private bool useCurve_y;
        private AnimationCurve curveMovement_y;
        private Vector3 positionFiredAt;
        private Vector3 storedTotalDisplacement = Vector3.zero;

        [Header("Damage")]
        // DAMAGE -N
        [SerializeField] protected int bulletDamage;
        [SerializeField] protected bool destroyOnCollision = true;
        //[SerializeField] LayerMask layerMask;

        [Space(20)]
        [SerializeField] private TrailRenderer trailRenderer;
        [SerializeField] private BulletLaserTrailCollisions awesomeLaser;


        virtual protected void Awake()
        {
            defaultParent = transform.parent;
            
            speedModifier_default = speedModifier;
            if (bulletDamage <= 0) { bulletDamage = 1; } // -N
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        public void ClearTrailRenderer()
        {
            if(trailRenderer) trailRenderer.Clear();
        }
        
        public void SetTransformations(Vector3 pos, Quaternion rot)
        {
            transform.position = pos;
            transform.rotation = rot;
            ClearTrailRenderer();
        }

        //The velocity given to a bullet will use X and Y coordinates AS IF it is in '2D space', perspective will be processed later
        public virtual void
            OnCreateBullet(Vector3 v, Vector3 a, ETempPerspective p = ETempPerspective.eTopDown) //regular version
        {

            ResetValues();

            typeOfBulletMovement = ETypeOfBulletMovement.eRegular;

            velocity = v;
            acceleration = a;
            perspectiveMovement = p;

            positionSpawned = transform.position;
            if(awesomeLaser) awesomeLaser.ResetTimer();
            
            ClearTrailRenderer();
        }

        //only takes X curve
        //when curve values are passed in to be used as coordinates, 'X' is relative to which horizontal axis goes with the current perspective and likewise for 'Y' and vertical
        public void OnCreateBullet(float curveScalarValue, AnimationCurve curve, Vector3 newVelocity, Vector3 newAccel,
            bool onXAxis = true,
            ETempPerspective p =
                ETempPerspective
                    .eTopDown) // if just one curve is used,  velocity and accel is still needed for the other axis
        {
            //Within this type of movement processing, the swapping of planes must actually be considered based off the perspective,
            //unless later movement on the third axis via a curve is added

            ResetValues();

            typeOfBulletMovement = ETypeOfBulletMovement.eUseCurve;

            velocity = newVelocity;
            acceleration = newAccel;

            curveMagnitude = curveScalarValue;

            if (onXAxis)
            {
                useCurve_x = true;
                useCurve_y = false;

                curveMovement_x = curve;
            }
            else
            {
                useCurve_x = false;
                useCurve_y = true;

                curveMovement_y = curve;
            }

            //needs additional values to handle rotation

            //use the velocity value to determine what 'angle' the bullet is traveling in
            //also use the velocity components for movement that isn't linked to a curve
            //do later

            positionFiredAt = transform.position;
            if(awesomeLaser) awesomeLaser.ResetTimer();
            
            ClearTrailRenderer();
        }

        public void OnCreateBullet(float curveScalarValue, AnimationCurve curveX, AnimationCurve curveY,
            Vector3 newVelocity, Vector3 newAccel,
            ETempPerspective p =
                ETempPerspective
                    .eTopDown) // if just one curve is used,  velocity and accel is still needed for the other axis
        {
            //I don't know if velocity and acceleration passed into this function are entirely necessary but might be useful to keep

            ResetValues();


            typeOfBulletMovement = ETypeOfBulletMovement.eUseCurve;

            velocity = newVelocity;
            acceleration = newAccel;

            curveMagnitude = curveScalarValue;

            useCurve_x = true;
            useCurve_y = true;

            curveMovement_x = curveX;
            curveMovement_y = curveY;

            positionFiredAt = transform.position;
            
            
            positionSpawned = transform.position;
            if(awesomeLaser) awesomeLaser.ResetTimer();
            
            ClearTrailRenderer();
        }

        private void CheckForDespawnDistance()
        {
            if (Mathf.Abs(transform.position.x - positionSpawned.x) > maxDespawnDist_x)
            {
                gameObject.SetActive(false);
            }
            if (Mathf.Abs(transform.position.y - positionSpawned.y) > maxDespawnDist_y)
            {
                gameObject.SetActive(false);
            }
            if (Mathf.Abs(transform.position.z - positionSpawned.z) > maxDespawnDist_z)
            {
                gameObject.SetActive(false);
            }
        }


        //Use when retrieving from pool
        public virtual void ResetValues()
        {
            lifetime = 0;
            velocity = Vector3.zero;
            acceleration = Vector3.zero;
            storedTotalDisplacement = Vector3.zero;

            speedModifier = speedModifier_default;

            useCurve_x = false;
            useCurve_y = false;
        }

        private void Reset()
        {
            Debug.Log("Reset test");
            if(trailRenderer) trailRenderer.Clear();
        }

        // Update is called once per frame
        void Update()
        {

            HandleBulletMovement();


            lifetime += Time.deltaTime;
            
            CheckForDespawnDistance();

        }

        private void OnTriggerEnter(Collider other) //use ontrigger stay instead?
        {
            //Debug.Log("COLLIDE  " + other.tag + "   " + other.transform.parent);
            //Debug.Log(other.transform.parent);
            if (other.CompareTag("Enemy"))
            {
                //Debug.Log($"ENEMY HIT {other.gameObject.name}");
                //Debug.Log(other.GetType());
                EnemyBase enemyHit = other.GetComponent<EnemyBase>(); // used to give damage -N
                OnHitEnemy(enemyHit);
            }

        }

        public virtual void HandleBulletMovement()
        {
            switch (typeOfBulletMovement)
            {
                case ETypeOfBulletMovement.eRegular:
                    transform.position += velocity * speedModifier * Time.deltaTime;
                    velocity += acceleration * Time.deltaTime; //should this be deltatimed?
                    //SPEEDDECAY is an overall slowdown. Probably should not be used if acceleration isn't going to be 0.
                    speedModifier -=
                        speedDecay *
                        Time.deltaTime; //should I used the half deltatime trick here like I saw in the video
                    if (speedModifier <= 0) speedModifier = 0;

                    //have an option for speed decay to not stop at 0, maybe for boomerang kind of bullets



                    if (stopMovingAtZeroVelocity_x) velocity.x = Mathf.Max(velocity.x, 0);
                    if (stopMovingAtZeroVelocity_y) velocity.y = Mathf.Max(velocity.y, 0);
                    if (stopMovingAtZeroVelocity_z) velocity.z = Mathf.Max(velocity.z, 0);

                    break;
                case ETypeOfBulletMovement.eUseCurve:

                    Vector3 displacement = Vector3.zero;

                    float dX = 0, dY = 0;
                    Vector3 velocityMovement = Vector3.zero;

                    if (useCurve_x)
                    {
                        dX = curveMovement_x.Evaluate(lifetime) * curveMagnitude;
                    }

                    if (useCurve_y)
                    {
                        dY = curveMovement_y.Evaluate(lifetime) * curveMagnitude;
                    }


                    switch (perspectiveMovement)
                    {
                        case ETempPerspective.eTopDown:

                            displacement.x = dX;
                            //velocity.y = v.z;   //IRRELEVANT AXIS
                            displacement.z = dY;

                            velocityMovement.x = velocity.x;
                            velocityMovement.z = velocity.z;

                            break;

                        case ETempPerspective.eSideOn:

                            // velocity.x = v.z;   //IRRELEVANT AXIS
                            displacement.y = dY;
                            displacement.z = dX;
                            ;

                            velocityMovement.y = velocity.y;
                            velocityMovement.z = velocity.z;

                            break;

                        case ETempPerspective.eOnRails:

                            //Less priority perspective don't worry too much

                            displacement.x = dX;
                            ;
                            displacement.y = dY;
                            //velocity.z = v.z;   //IRRELEVANT AXIS

                            velocityMovement.x = velocity.y;
                            velocityMovement.y = velocity.z;

                            break;

                        default:

                            break;
                    }


                    transform.position = positionFiredAt + displacement;
                    //by default just add given velocity anyway. Any axis that is linked to a curve should be on 0 anyway

                    storedTotalDisplacement += velocity * Time.deltaTime;
                    //handle acceleration if I get around to adding it
                    transform.position += storedTotalDisplacement;


                    //Deal with this junkk later it's not needed until a very specific pattern calls for it
                    /*if (!useCurve_x)
                    {
                        transform.position += new Vector3(velocity.x, 0, 0);
                    }
                    */


                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected virtual void OnHitEnemy(EnemyBase enemyHit)
        {
            if(destroyOnCollision) BecomeDormant();
            enemyHit.TakeDamage(bulletDamage); // take dmg -N (based of jonnys implementation for enemy health)
        }

        public void SetVelocityToForwardRotation()
        {
            velocity = transform.forward;
        }


        public void BecomeDormant() //probably don't use this because returning to pool queue manually might cause circular queue issues where something is enqueued more than once
        {
            BulletPool.Instance.ReturnToPool(this);
        }

        protected virtual void StopBeingActive() //use this instead and the pool will come to retrieve the bullet when needs to
        {
            gameObject.SetActive(false);
        }

        public void SetSpeedDecay(float decay)
        {
            speedDecay = decay;
        }

        public void ReturnToDefaultParent()
        {
            if (defaultParent == null)
            {
                //Debug.LogWarning("WARNING: Bullet could not return to default parent as it is null");
            }
            else
            {
                transform.parent = defaultParent;
            }
        }

        private void OnDrawGizmos()
        {
            /*Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, transform.right);
            Gizmos.DrawRay(transform.position, -transform.right);
            Gizmos.DrawRay(transform.position, transform.forward);
            Gizmos.DrawRay(transform.position, -transform.forward);*/
        }
    }

}