﻿using System.Collections.Generic;
using GBJAM.Commons;
using GBJAM.Commons.Menus;
using UnityEngine;

namespace GBJAM7.Scripts.MainMenu
{
    public class MainMenuSceneController : MonoBehaviour
    {
        public OptionsMenu options;

        public GameboyButtonKeyMapAsset keyMapAsset;

        private bool showingOptions;

        public LevelDefinitionAsset[] levels;

        public MainMenuIntro mainMenuIntro;

        [SerializeField]
        private AudioSource startButtonSfx;

        private void Start()
        {
            options.title = "Pick stage";
        }

        public void Update()
        {
            keyMapAsset.UpdateControlState();

            if (!mainMenuIntro.completed)
            {
                if (keyMapAsset.AnyButtonPressed())
                {
                    mainMenuIntro.ForceComplete();
                    return;
                }
            }
            
            if (showingOptions)
            {

            } else {
                if (keyMapAsset.AnyButtonPressed())
                {
                    if (startButtonSfx != null)
                    {
                        startButtonSfx.Play();
                    }
                    
                    mainMenuIntro.HideStart();
                    
                    showingOptions = true;
                    var optionsList = new List<Option>();
                    foreach (var level in levels)
                    {
                        optionsList.Add(new Option()
                        {
                            name = level.name
                        });
                    }
                    options.Show(optionsList, OnOptionSelected, OnCancel);
                }
            }
        }

        private void OnCancel()
        {
            options.Hide();
            showingOptions = false;
            mainMenuIntro.ShowStart();
        }

        private bool OnOptionSelected(int arg1, Option option)
        {
            foreach (var level in levels)
            {
                if (level.name.Equals(option.name))
                {
                    ScenesLoader.LoadLevel(level);
                }
            }
            
            // TODO: maybe add a help button here??

//            if ("New Game".Equals(option.name))
//            {
//                
//            }
//            
//            if ("Credits".Equals(option.name))
//            {
//                
//            }

            return true;
        }
    }
}
