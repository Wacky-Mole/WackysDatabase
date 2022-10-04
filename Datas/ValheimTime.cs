using System;
using UnityEngine;

namespace wackydatabase.Datas
{
    [Serializable]
    public class ValheimTime
    {
        private const int DAY_LENGTH = 86400;

        public int Hour { get; }
        public int Minute { get; }
        public int Second { get; }

        public int Day { get; }

        public ValheimTime(int hour = 0, int minute = 0, int second = 0, int day = -1)
        {
            Day = day;
            Hour = hour;
            Minute = minute;
            Second = second;
        }

        public string ToDataString()
        {
            return Hour + ":" + Minute + ":" + Second + " (" + Day + ")";
        }

        public int ToSeconds()
        {
            return (int)new TimeSpan(Hour, Minute, Second).TotalSeconds;
        }

        public static float RatioUntilTime(ValheimTime a, ValheimTime b, ValheimTime span)
        {
            if (Mathf.Abs(a.ToSeconds() - b.ToSeconds()) < span.ToSeconds())
            {
                float diff = Mathf.Abs(a.ToSeconds() - b.ToSeconds());

                return 1.0f - (diff / span.ToSeconds());
            }
            else if (Mathf.Abs(a.ToSeconds() + DAY_LENGTH - b.ToSeconds()) < span.ToSeconds())
            {
                float diff = Mathf.Abs(a.ToSeconds() + DAY_LENGTH - b.ToSeconds());

                return 1.0f - (diff / span.ToSeconds());
            }
            else if (Mathf.Abs(a.ToSeconds() - (b.ToSeconds() + DAY_LENGTH)) < span.ToSeconds())
            {
                float diff = Mathf.Abs(a.ToSeconds() - (b.ToSeconds() + DAY_LENGTH));

                return 1.0f - (diff / span.ToSeconds());
            }

            return 0;
        }

        public static ValheimTime Get()
        {
            if (!EnvMan.instance)
                return new ValheimTime(-1);

            float time = EnvMan.instance.m_smoothDayFraction;
            int day = EnvMan.instance.GetCurrentDay();

            int hour = (int)(time * 24);
            int minute = (int)((time * 24 - hour) * 60);
            int second = (int)((((time * 24 - hour) * 60) - minute) * 60);

            return new ValheimTime(hour, minute, second, day);
        }
    }
}
