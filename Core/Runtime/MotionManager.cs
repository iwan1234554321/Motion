using System.Collections.Generic;
using UnityEngine;

namespace Notteam.Motion
{
    /// <summary>
    /// Менеджер моушен компонентов
    /// </summary>
    public static class MotionManager
    {
        private static readonly Dictionary<MotionKey, MotionComponent>    motions       = new();
        private static readonly Dictionary<int, HashSet<MotionComponent>> targetMotions = new();

        /// <summary>
        /// Функция создания моушен компонента
        /// </summary>
        /// <param name="target">Целевой MonoBehaviour который закрепляется за моушен компонентом</param>
        /// <param name="id">Идентификатор моушен компонента</param>
        /// <param name="replace">Флаг замены моушен компонента</param>
        /// <returns>Возвращает моушен компонент</returns>
        public static MotionComponent Motion(
            this MonoBehaviour target,
            string id = MotionUtility.DefaultMotionID,
            bool replace = false)
        {
            var key = new MotionKey(id, target);

            return new MotionComponent(key, replace);
        }

        /// <summary>
        /// Регистрация моушен компонента в менеджере моушен компонентов
        /// </summary>
        /// <param name="motion">Моушен компонент</param>
        internal static void RegisterMotion(MotionComponent motion)
        {
            var key = motion.Key;

            var instanceId = key.Target.gameObject.GetInstanceID();

            if (motions.TryGetValue(key, out var similarMotion))
            {
                if (motion.Replace)
                {
                    similarMotion.ReplaceMotion(motion);

                    return;
                }
                else
                {
                    similarMotion.Stop();
                }
            }

            motions[key] = motion;

            if (!targetMotions.TryGetValue(instanceId, out var group))
            {
                group = new HashSet<MotionComponent>();

                targetMotions[instanceId] = group;
            }

            group.Add(motion);
        }

        /// <summary>
        /// Отписка моушен компонента от менеджера
        /// </summary>
        /// <param name="motion">Моушен компонент</param>
        internal static void UnregisterMotion(MotionComponent motion)
        {
            var key = motion.Key;

            var instanceId = key.Target.gameObject.GetInstanceID();

            if (motions.TryGetValue(key, out var similarMotion))
            {
                motions.Remove(key);

                if (targetMotions.TryGetValue(instanceId, out var group))
                {
                    group.Remove(similarMotion);

                    if (group.Count == 0)
                        targetMotions.Remove(instanceId);
                }
            }
        }

        /// <summary>
        /// Остановка моушен компонента
        /// </summary>
        /// <param name="target">Целевой MonoBehaviour который закрепляется за моушен компонентом</param>
        /// <param name="id">Идентификатор моушен компонента</param>
        /// <param name="toDefault">Флаг установки начального значения моушена</param>
        public static void StopMotion(this MonoBehaviour target, string id = MotionUtility.DefaultMotionID, bool toDefault = false)
        {
            var key = new MotionKey(id, target);

            if (motions.TryGetValue(key, out var motion))
            {
                motion.Stop(toDefault);
            }
        }

        /// <summary>
        /// Остановить все моушен компоненты на текущем MonoBehaviour
        /// </summary>
        /// <param name="target">Целевой MonoBehaviour который закрепляется за моушен компонентом</param>
        /// <param name="toDefault">Флаг установки начального значения моушена</param>
        public static void StopAllMotions(this MonoBehaviour target, bool toDefault = false)
        {
            var instanceId = target.gameObject.GetInstanceID();

            if (!targetMotions.TryGetValue(instanceId, out var group))
                return;

            foreach (var motion in group)
            {
                motion.Stop(toDefault);
            }

            targetMotions.Remove(instanceId);
        }

        /// <summary>
        /// Остановка моушен компонента на конечном значении
        /// </summary>
        /// <param name="target">Целевой MonoBehaviour который закрепляется за моушен компонентом</param>
        /// <param name="id">Идентификатор моушен компонента</param>
        public static void CompleteMotion(this MonoBehaviour target, string id = MotionUtility.DefaultMotionID)
        {
            var key = new MotionKey(id, target);

            if (motions.TryGetValue(key, out var motion))
            {
                motion.Complete();
            }
        }
    }
}