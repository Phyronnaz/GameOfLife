﻿using System;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts
{
    public class MainUI : MonoBehaviour
    {
        public Button SaveButton;
        public Button LoadButton;

        public Text DensityText;
        public Slider RandomizeSlider;
        public Button SingleButton;
        public Button RandomizeButton;

        public InputField WField;
        public InputField XField;
        public InputField YField;
        public InputField ZField;

        public InputField ChunkSizeField;
        public InputField ThreadSizeField;

        public InputField SizeField;
        public Button ChangeSizeButton;

        public Toggle VSyncToggle;
        public Toggle DebugToggle;
        public Toggle Use3DToggle;
        public Toggle EditModeToggle;
        public Toggle ConstantUpdateToggle;

        public Slider UpdateTimeSlider;
        public Text UpdateTimeText;

        public Image WorkingImage;

        public static MainUI UI;

        void Start()
        {
            UI = this;

            SaveButton.onClick.AddListener(IO.Save);
            LoadButton.onClick.AddListener(IO.Load);

            RandomizeSlider.onValueChanged.AddListener(f => DensityText.text = string.Format("Density: {0}", Mathf.Pow(RandomizeSlider.value, 2) + 0.0001f).Substring(0, 13));
            RandomizeButton.onClick.AddListener(() => GameOfLifeTools.Randomize(Mathf.Pow(RandomizeSlider.value, 2)));
            SingleButton.onClick.AddListener(GameOfLifeTools.SingleBlock);


            WField.onEndEdit.AddListener(s => { if (s != "") GameOfLifeTools.W = int.Parse(s); else WField.text = GameOfLifeTools.W.ToString(); });
            XField.onEndEdit.AddListener(s => { if (s != "") GameOfLifeTools.X = int.Parse(s); else XField.text = GameOfLifeTools.X.ToString(); });
            YField.onEndEdit.AddListener(s => { if (s != "") GameOfLifeTools.Y = int.Parse(s); else YField.text = GameOfLifeTools.Y.ToString(); });
            ZField.onEndEdit.AddListener(s => { if (s != "") GameOfLifeTools.Z = int.Parse(s); else ZField.text = GameOfLifeTools.Z.ToString(); });

            WField.onEndEdit.Invoke("");
            XField.onEndEdit.Invoke("");
            YField.onEndEdit.Invoke("");
            ZField.onEndEdit.Invoke("");

            ChunkSizeField.onEndEdit.AddListener(s =>
            {
                if (s != "")
                {
                    GameOfLifeTools.ChunkSize = int.Parse(s);
                    Log.LogWarning("Restart needed");
                    ChunkSizeField.text = GameOfLifeTools.ChunkSize.ToString();
                }
                else
                {
                    ChunkSizeField.text = GameOfLifeTools.ChunkSize.ToString();
                }
            });
            ThreadSizeField.onEndEdit.AddListener(s => { if (s != "") GameOfLifeTools.ThreadSize = int.Parse(s); else ThreadSizeField.text = GameOfLifeTools.ThreadSize.ToString(); });

            ChunkSizeField.onEndEdit.Invoke("");
            ThreadSizeField.onEndEdit.Invoke("");

            SizeField.text = GameOfLife.GOL.Size.ToString();
            ChangeSizeButton.onClick.AddListener(() => GameOfLifeTools.SetSize(int.Parse(SizeField.text)));

            VSyncToggle.onValueChanged.AddListener(b => QualitySettings.vSyncCount = b ? 1 : 0);
            DebugToggle.onValueChanged.AddListener(b => Log.Enabled = b);
            Use3DToggle.onValueChanged.AddListener(b => GameOfLife.Use3D = b);
            EditModeToggle.onValueChanged.AddListener(b => GameOfLifeTools.SetEditMode(b));
            ConstantUpdateToggle.onValueChanged.AddListener(b => GameOfLifeHandler.ConstantUpdate = b);

            UpdateTimeSlider.onValueChanged.AddListener(f =>
            {
                UpdateTimeText.text = string.Format("Update Time: {0}", UpdateTimeSlider.value * 0.05f + 0.0001f).Substring(0, 17) + "s";
                GameOfLifeHandler.UpdateTime = UpdateTimeSlider.value * 0.05f;
            });

            GameOfLifeHandler.WorkingImage = WorkingImage;
        }
    }
}
