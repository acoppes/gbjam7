﻿using System;
using UnityEngine;

namespace GBJAM7.Scripts
{
    [ExecuteInEditMode]
    public class UnitSelector : MonoBehaviour
    {
        public Vector3Int movement = new Vector3Int(1, 1, 0);

        [NonSerialized]
        public Vector3 position;

        public float moveSpeed = 1.0f;
        
        public Transform spriteTransform;

        [SerializeField]
        private AudioSource _selectorMoveSfx;

        public void Start()
        {
            transform.position = Vector3.zero;
        }
        
        public void Move(Vector2Int direction)
        {
            position += new Vector3(movement.x * direction.x, movement.y * direction.y, 0);
            if (Mathf.Abs(direction.x) > 0 || Mathf.Abs(direction.y) > 0)
            {
                if (_selectorMoveSfx != null)
                {
                    _selectorMoveSfx.Play();
                }
            }
        }

        public void LateUpdate()
        {
            if (Application.isPlaying)
            {
                spriteTransform.transform.position = Vector3.Lerp(spriteTransform.transform.position, position,
                    moveSpeed * Time.deltaTime);
            }
            else
            {
                position = transform.position;
                spriteTransform.transform.position = position;
            }
        }
    }
}
