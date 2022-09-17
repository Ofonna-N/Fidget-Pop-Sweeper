using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using TMPro;

namespace FidgetSweeper
{
    public class Test : MonoBehaviour
    {
        Stopwatch stopwatch;

        System.TimeSpan ts;

        public TextMeshProUGUI timeText;

        public TextMeshProUGUI seconds;
        // Start is called before the first frame update
        void Start()
        {
            stopwatch = new Stopwatch();
            stopwatch.Start();
        }

        // Update is called once per frame
        void Update()
        {
            if (stopwatch == null) return;
            ts = stopwatch.Elapsed;

            timeText.text = string.Format("{0:00}:{1:00}", ts.Minutes, ts.Seconds);

            seconds.text = ((int)ts.TotalSeconds).ToString();
            //timeText.text = $"{ts.Minutes}";
        }

        public void StopTime()
        {
            stopwatch.Stop();
        }

        public void StartTime()
        {
            stopwatch.Start();
        }
    }
}
