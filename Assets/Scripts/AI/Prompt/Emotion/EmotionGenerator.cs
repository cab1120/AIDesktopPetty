using System;
using System.Collections.Generic;
using UnityEngine;

public static class EmotionGenerator
{
    public static EmotionData GenerateEmotion()
    {
        DateTime now = DateTime.Now;

        int hour = now.Hour;
        DayOfWeek day = now.DayOfWeek;

        bool isWeekend =
            day == DayOfWeek.Saturday ||
            day == DayOfWeek.Sunday;

        Dictionary<EmotionType, int> weights =
            new Dictionary<EmotionType, int>();

        // 初始化
        foreach (EmotionType type in Enum.GetValues(typeof(EmotionType)))
        {
            weights[type] = 1;
        }

        // =========================
        // 时间段权重
        // =========================

        // 深夜
        if (hour >= 23 || hour < 5)
        {
            weights[EmotionType.Sleepy] += 30;
            weights[EmotionType.Lonely] += 20;
            weights[EmotionType.Nostalgic] += 20;
            weights[EmotionType.Soft] += 15;
            weights[EmotionType.Tired] += 25;
        }
        // 早晨
        else if (hour >= 6 && hour <= 9)
        {
            weights[EmotionType.Sleepy] += 20;
            weights[EmotionType.Protective] += 20;
            weights[EmotionType.Focused] += 15;
        }
        // 白天
        else if (hour >= 10 && hour <= 17)
        {
            weights[EmotionType.Focused] += 25;
            weights[EmotionType.Calm] += 20;
            weights[EmotionType.Tired] += 10;
        }
        // 黄昏
        else if (hour >= 18 && hour <= 21)
        {
            weights[EmotionType.Creative] += 30;
            weights[EmotionType.Nostalgic] += 15;
            weights[EmotionType.Soft] += 15;
            weights[EmotionType.SlightlyHappy] += 10;
        }
        // 深夜前
        else
        {
            weights[EmotionType.Calm] += 20;
            weights[EmotionType.Tired] += 20;
            weights[EmotionType.Soft] += 10;
        }
        // =========================
        // 工作日 / 周末修正
        // =========================

        if (isWeekend)
        {
            // 周末的彩叶会稍微放松一点
            // 更容易陪用户聊天

            weights[EmotionType.Soft] += 15;
            weights[EmotionType.Creative] += 15;
            weights[EmotionType.SlightlyHappy] += 20;

            // 周末熬夜概率上升
            if (hour >= 23 || hour < 3)
            {
                weights[EmotionType.Creative] += 15;
            }
        }
        else
        {
            // 工作日的彩叶会更“硬撑”
            // 更接近原作那种疲惫感

            weights[EmotionType.Focused] += 20;
            weights[EmotionType.Tired] += 25;
            weights[EmotionType.Protective] += 10;

            // 周一额外疲惫
            if (day == DayOfWeek.Monday)
            {
                weights[EmotionType.Tired] += 15;
                weights[EmotionType.Sleepy] += 10;
            }
        }
        // =========================
        // 情绪惯性系统
        // =========================

        EmotionData previous = EmotionMemory.GetCurrentEmotion();

        if (previous != null)
        {
            // 上一个情绪会获得额外权重
            // 避免人格跳变

            weights[previous.CurrentEmotion] += 35;

            // 某些情绪之间会互相延续

            switch (previous.CurrentEmotion)
            {
                case EmotionType.Tired:
                    weights[EmotionType.Sleepy] += 10;
                    break;

                case EmotionType.Nostalgic:
                    weights[EmotionType.Soft] += 10;
                    break;

                case EmotionType.Creative:
                    weights[EmotionType.Focused] += 10;
                    break;

                case EmotionType.Lonely:
                    weights[EmotionType.Protective] += 10;
                    break;
            }
        }

        // =========================
        // 随机抽取
        // =========================

        EmotionType selected = WeightedRandom(weights);

        EmotionData result = new EmotionData()
        {
            CurrentEmotion = selected,

            // 彩叶情绪强度不会特别高
            Intensity = UnityEngine.Random.Range(0.35f, 0.8f),

            // 持续时间
            RemainingMinutes = UnityEngine.Random.Range(40, 180),
            
        };
        result.SetLastUpdate(now);
        EmotionMemory.SetEmotion(result);

        return result;
    }
    private static EmotionType WeightedRandom(
        Dictionary<EmotionType, int> weights)
    {
        int total = 0;

        foreach (var pair in weights)
        {
            total += pair.Value;
        }

        int random = UnityEngine.Random.Range(0, total);

        int current = 0;

        foreach (var pair in weights)
        {
            current += pair.Value;

            if (random < current)
            {
                return pair.Key;
            }
        }

        return EmotionType.Calm;
    }
}