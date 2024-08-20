using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace NiBullets
{

    [Serializable]
    struct QueuedBulletType
    {
        public int bulletID;
        public GameObject bulletPrefab;
        public int count;

    }

    public class BulletPool : MonoBehaviour
    {
        public static BulletPool Instance { get; private set; }

        [SerializeField] QueuedBulletType[] bulletTypes;

        private Dictionary<int, Queue<Bullet>> bulletDictionary_dormant = new Dictionary<int, Queue<Bullet>>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                //DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }

        }


        // Start is called before the first frame update
        void Start()
        {
            #region Setup object pool

            foreach (QueuedBulletType bulletData in bulletTypes) //loop through all the types of bullets in the game
            {
                //Create empty queue for new bullets
                Queue<Bullet> newQueue = new Queue<Bullet>();
                Transform newXform = new GameObject().transform;
                newXform.name = bulletData.bulletPrefab.name;
                newXform.position = transform.position;
                newXform.parent = transform;

                for (int j = 0;
                    j < bulletData.count;
                    j++) //create the given amount of each type of bullet and add all to queue
                {
                    Bullet newBullet = Instantiate(bulletData.bulletPrefab, transform.position, Quaternion.identity)
                        .GetComponent<Bullet>();
                    newBullet.gameObject.SetActive(false);
                    newBullet.transform.parent = newXform;
                    //newBullet.transform.parent = transform;

                    newQueue.Enqueue(newBullet);
                }

                //add queue of bullets to lookup container of inactive bullets
                bulletDictionary_dormant.Add(bulletData.bulletID, newQueue);
                //adds corresponding entry  dictionary for active bullets
                /*Queue<Bullet> emptyQueue = new Queue<Bullet>();
                bulletDictionary_alive.Add(bulletData.bulletID, emptyQueue);*/

            }

            #endregion

        }

        // Update is called once per frame
        void Update()
        {

        }

        public Bullet RetrieveBullet(int bulletID = 0)
        {
            Bullet newBullet;

            if (bulletDictionary_dormant[bulletID].TryDequeue(out newBullet))
            {
                newBullet.gameObject.SetActive(true);
                bulletDictionary_dormant[bulletID].Enqueue(newBullet);
                return newBullet;
            }
            /*else if (bulletDictionary_alive[bulletID].TryDequeue(out newBullet))    //should not be necessary
            {
                bulletDictionary_alive[bulletID].Enqueue(newBullet);
                return newBullet;
            }*/
            else
            {
                Debug.LogWarning("WARNING, NO BULLET RETRIEVED");
                return newBullet;
            }

        }

        public void ReturnToPool(Bullet b)
        {
            b.ResetValues(); //doesn't need to be a public function
            bulletDictionary_dormant[b.publicBulletID].Enqueue(b);
            b.gameObject.SetActive(false);
        }
    }

}
