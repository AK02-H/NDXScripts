using System;
using System.Collections;
using System.Collections.Generic;
using PixelDust.Audiophile;
using UnityEngine;

namespace NiBullets
{
    public class Bullet_Split : Bullet
    {
        [Header("Split")]
        [SerializeField] private Transform scrollerTransform;
        [SerializeField] private int bulletsToSplit = 6;
        [SerializeField] private ParticleSystem vfxBulletSpark;
        [SerializeField] private Bullet splitOffBullets;
        [SerializeField] private float speedDecayForSplitBullets;
        [SerializeField] private SoundEvent sfx_splitSound;

        protected override void Awake()
        {
            base.Awake();
            scrollerTransform = GameObject.FindGameObjectWithTag("ScrollerBulletHolder").transform; //possibly inneficient also weird that things will become messy in the scroller gameobject hierarchy
        }

        //[SerializeField] Vector3 
        protected override void OnHitEnemy(EnemyBase enemyHit) // pass through needed for damage (see base class OnHitEnemy()) -N
        {
            
            //Debug.Break();
            //GetComponent<TrailRenderer>().res

            switch (GameManager.Instance.perspectiveState)
            {
                case EGamePerspective.ETopDown:
                    for (int i = 0; i < bulletsToSplit; i++)
                    {
                        Vector3 angle = Quaternion.AngleAxis(0 + (360 / bulletsToSplit * i), Vector3.up) * velocity; //make axis depend on perspective

                        Bullet newBullet = BulletPool.Instance.RetrieveBullet(splitOffBullets.publicBulletID);
                        newBullet.ClearTrailRenderer();
                        newBullet.transform.position = transform.position;
                        newBullet.GetComponent<TrailRenderer>().Clear();
                        newBullet.OnCreateBullet(angle, Vector3.zero);
                        newBullet.SetSpeedDecay(speedDecayForSplitBullets);
                        newBullet.transform.parent = scrollerTransform;
                    }
                    break;
                case EGamePerspective.ERightView:
                    for (int i = 0; i < bulletsToSplit; i++)
                    {
                        Vector3 angle = Quaternion.AngleAxis(0 + (360 / bulletsToSplit * i), Vector3.right) * velocity; //make axis depend on perspective

                        Bullet newBullet = BulletPool.Instance.RetrieveBullet(splitOffBullets.publicBulletID);
                        newBullet.transform.position = transform.position;
                        newBullet.GetComponent<TrailRenderer>().Clear();
                        newBullet.OnCreateBullet(angle, Vector3.zero);
                        newBullet.SetSpeedDecay(speedDecayForSplitBullets);
                        newBullet.transform.parent = scrollerTransform;
                    }
                    break;
                case EGamePerspective.EOnRails:
                    break;
                case EGamePerspective.ECutscene:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            
            /*Vector3 angle1 = Quaternion.AngleAxis(90, Vector3.up) * velocity; //make axis depend on perspective
            Vector3 angle2 = Quaternion.AngleAxis(-90, Vector3.up) * velocity; //make axis depend on perspective

            Bullet newBullet = BulletPool.Instance.RetrieveBullet(splitOffBullets.publicBulletID);
            newBullet.transform.position = transform.position;
            newBullet.GetComponent<TrailRenderer>().Clear();
            newBullet.OnCreateBullet(angle1, Vector3.zero);
            newBullet.SetSpeedDecay(speedDecayForSplitBullets);
            //newBullet.GetComponent<ParticleSystem>().Stop();
            //newBullet.GetComponent<ParticleSystem>().Play();

            Bullet newBullet2 = BulletPool.Instance.RetrieveBullet(splitOffBullets.publicBulletID);
            newBullet2.transform.position = transform.position;
            newBullet2.GetComponent<TrailRenderer>().Clear();
            newBullet2.OnCreateBullet(angle2, Vector3.zero);
            newBullet2.SetSpeedDecay(speedDecayForSplitBullets);*/
            //newBullet2.GetComponent<ParticleSystem>().Stop();
            //newBullet2.GetComponent<ParticleSystem>().Play();

            sfx_splitSound.Play();
            enemyHit.TakeDamage(bulletDamage);
            
            StopBeingActive();
        }

        private void OnDisable()
        {
            //Debug.Break();

        }
        
        protected override void StopBeingActive() //use this instead and the pool will come to retrieve the bullet when needs to
        {
            vfxBulletSpark.Stop();
            gameObject.SetActive(false);
        }
    }
}
