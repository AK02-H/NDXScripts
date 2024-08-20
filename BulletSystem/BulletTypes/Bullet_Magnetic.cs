using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace NiBullets
{
    public class Bullet_Magnetic : Bullet
    {

        [Header("Magnetic")] [SerializeField] private float attractionRange;
        [SerializeField] private float rotateSpeed = 20;
        [SerializeField] private float timeBeforeMagnetActivates = 0.5f; //don't change in code
        private float timeBeforeMagnetActivates_timer = 0.5f;


        [SerializeField] private LayerMask enemyMask;
        private float scalarAcceleration;
        private bool foundTarget = false;
        private GameObject magnetTarget;

        private float timeSinceFoundTarget = 0; //use animation curve for it

        private float existenceTime = 0;
        private float maxExistenceTime = 6;
        
        protected override void Awake()
        {
            base.Awake();

        }

        public override void OnCreateBullet(Vector3 v, Vector3 a, ETempPerspective p = ETempPerspective.eTopDown)
        {
            base.OnCreateBullet(v, a, p);

            Quaternion newDirection = Quaternion.LookRotation(velocity);
            transform.rotation = newDirection;
            //velocity = transform.forward * speedModifier; // needed?
            scalarAcceleration = a.sqrMagnitude; //saves processing
            //velocity not needed after this

            timeBeforeMagnetActivates_timer = timeBeforeMagnetActivates;

            
        }

        public override void HandleBulletMovement()
        {
            if (timeBeforeMagnetActivates > 0)
            {
                timeBeforeMagnetActivates -= Time.deltaTime;
            }
            else
            {
                if (!foundTarget)
                {
                    Collider[] enemies =
                        Physics.OverlapSphere(transform.position, attractionRange,
                            enemyMask); //maybe don't bother sorting by the closest because it might take long

                    
                    List<EnemyBase> enemyComponents = new List<EnemyBase>();

                    foreach (var VARIABLE in enemies)
                    {
                        if (VARIABLE.GetComponent<EnemyBase>())
                        {
                            enemyComponents.Add(VARIABLE.GetComponent<EnemyBase>());
                        }
                    }
                    
                    
                    if (enemyComponents.Count > 0)
                    {
                        magnetTarget = enemyComponents[Random.Range(0, enemyComponents.Count() - 1)].gameObject;
                        foundTarget = true;
                        //Debug.Log("Found target");
                    }
                }
                else
                {
                    //either zero out the plane coordinates or make sure all enemies are on the same plane
                    //or maybe it won't matter
                    Vector3 targetDirection = magnetTarget.transform.position - transform.position;
                    Vector3 newRotation = Vector3.RotateTowards(transform.forward, targetDirection,
                        rotateSpeed * timeSinceFoundTarget * Time.deltaTime, 0); //look up what the last parameter does
                    transform.rotation = Quaternion.LookRotation(newRotation);

                    timeSinceFoundTarget += Time.deltaTime;


                    if (magnetTarget.activeSelf == false)
                    {
                        foundTarget = false;
                        existenceTime = 999;
                    }
                }
            }


            transform.position += transform.forward * speedModifier * Time.deltaTime;
            //velocity += acceleration * Time.deltaTime;                      //should this be deltatimed?
            speedModifier += scalarAcceleration * Time.deltaTime; //apply the curve trick here when I have the time



            existenceTime += Time.deltaTime;
            if (existenceTime > maxExistenceTime)
            {
                //LeanTween.scale(gameObject, Vector3.zero, 0.4f).setEaseOutQuint().setOnComplete(KillMyself);
                KillMyself();
            }



            /*if (stopMovingAtZeroVelocity_x) velocity.x = Mathf.Max(velocity.x, 0);
            if (stopMovingAtZeroVelocity_y) velocity.y = Mathf.Max(velocity.y, 0);
            if (stopMovingAtZeroVelocity_z)  velocity.z = Mathf.Max(velocity.z, 0);*/



        }

        void KillMyself()
        {
            gameObject.SetActive(false);
            ResetValues();
            
        }

        public override void ResetValues()
        {
            base.ResetValues();
            transform.rotation = Quaternion.identity; //maybe move this to base function
            foundTarget = false;
            magnetTarget = null;
            timeBeforeMagnetActivates_timer = timeBeforeMagnetActivates;
            existenceTime = 0;
            transform.localScale = Vector3.one * 0.5f;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            //Gizmos.DrawWireSphere(transform.position, attractionRange);
        }
    }

}
