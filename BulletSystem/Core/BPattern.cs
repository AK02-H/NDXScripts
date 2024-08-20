using System;
using System.Collections;
using System.Collections.Generic;
using PixelDust.Audiophile;
using UnityEngine;

namespace  NiBullets
{
    public class BPattern : MonoBehaviour
    {
        public BEmitter[] emitterGroup;

        [SerializeField] private SoundEvent sfx_defaultPatternSound;
        
        private void Awake()
        {
            emitterGroup = GetComponentsInChildren<BEmitter>();
        }

        public void Fire()
        {
            foreach (var e in emitterGroup)
            {
                e.ActivateEmitter();
            }
            
            if (sfx_defaultPatternSound != null) sfx_defaultPatternSound.Play();
        }

        public void StopFiring()
        {
            foreach (var e in emitterGroup)
            {
                e.StopEmitter();
            }
            
            if (sfx_defaultPatternSound != null) sfx_defaultPatternSound.Stop();
        }
    }
}