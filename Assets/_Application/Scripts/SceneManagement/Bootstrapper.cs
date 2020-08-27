﻿using UnityEngine;

namespace GMTK2020.SceneManagement
{
    public class Bootstrapper : MonoBehaviour
    {
        private void Start()
        {
            SceneLoader.Instance.LoadSplashScene();
            Destroy(gameObject);
        }
    } 
}
