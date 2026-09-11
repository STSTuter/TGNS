using JohnStairs.RCC.Inputs;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FYP.UI
{
    public class CharacterRotate : MonoBehaviour
    {
        RPGInputActions inputAction;
        [SerializeField]
        Transform player;

        private void Awake()
        {
            inputAction = RPGInputManager.GetInputActions();
        }

        private void Update()
        {
            float rotation = transform.rotation.x + inputAction.Character.Strafe.ReadValue<float>();
            player.Rotate(Vector3.up, rotation * 180 * Time.deltaTime);
        }

        private void OnEnable()
        {
            player.rotation = Quaternion.identity;
        }

        private void OnDisable()
        {
            player.rotation = Quaternion.identity;
        }
    }
}