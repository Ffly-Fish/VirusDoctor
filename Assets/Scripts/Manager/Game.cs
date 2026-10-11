using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class Game : MonoBehaviour
{
    public static InputManager inputManager { get; private set; }
    public static AudioManager audioManager { get; private set; }

    private static Game instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        inputManager ??= new InputManager();
        audioManager ??= new AudioManager(transform);
    }

    private void Update()
    {
        inputManager.Update();
    }


}