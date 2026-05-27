using UnityEngine;

namespace Notteam.Motion
{
    /// <summary>
    /// Тип Ease функции
    /// </summary>
    public enum Ease
    {
        Linear,

        InSine,
        OutSine,
        InOutSine,

        InQuad,
        OutQuad,
        InOutQuad,

        InCubic,
        OutCubic,
        InOutCubic,

        InQuart,
        OutQuart,
        InOutQuart,

        InQuint,
        OutQuint,
        InOutQuint,

        InExpo,
        OutExpo,
        InOutExpo,
    }

    /// <summary>
    /// Тип воспроизведения
    /// </summary>
    public enum Playback
    {
        Once,
        Loop,
        PingPong,
        Hold,
    }

    /// <summary>
    /// Ключ моушен компонента
    /// </summary>
    public struct MotionKey
    {
        private string        _id;
        private MonoBehaviour _target;

        internal MotionKey(string id, MonoBehaviour target)
        {
            _id     = id;
            _target = target;
        }

        /// <summary>
        /// Идентификатор моушен компонента
        /// </summary>
        internal string        Id     => _id;
        /// <summary>
        /// Целевой MonoBehaviour к которому привязывается моушен компонент
        /// </summary>
        internal MonoBehaviour Target => _target;

        public override int GetHashCode() => (_id, _target).GetHashCode();
        public override bool Equals(object obj) => obj is MotionKey other && other._target == _target && other._id == _id;
    }

    /// <summary>
    /// Состояние физики моушен анимации
    /// </summary>
    internal struct MotionSpringState
    {
        private float _value;
        private float _velocity;

        public MotionSpringState(float value, float velocity)
        {
            _value    = value;
            _velocity = velocity;
        }

        /// <summary>
        /// Значение физического состояния
        /// </summary>
        internal float Value    { get => _value; set => _value = value; }
        /// <summary>
        /// Ускорение значения физического состояния
        /// </summary>
        internal float Velocity { get => _velocity; set => _velocity = value; }
    }

    /// <summary>
    /// Настройка физики моушен анимации
    /// </summary>
    public struct MotionSpringSettings
    {
        private float _stiffness;
        private float _damping;
        private float _bounce;

        private bool _clamp;

        private const float _defaultStiffness = 150.0f;
        private const float _defaultDamping   = 10.0f;

        public MotionSpringSettings(
            float stiffness = 0.0f,
            float damping = 0.0f,
            float bounce = 0.0f,
            bool clamp = false)
        {
            _stiffness = stiffness;
            _damping   = damping;
            _bounce    = bounce;

            _clamp     = clamp;
        }

        /// <summary>
        /// Жесткость пружины
        /// </summary>
        public float Stiffness => _stiffness > 0.0f ? _stiffness : _defaultStiffness;
        /// <summary>
        /// Амортизация пружины
        /// </summary>
        public float Damping   => _damping > 0.0f ? _damping : _defaultDamping;
        /// <summary>
        /// Отскок пружины
        /// </summary>
        public float Bounce    => _bounce;

        public bool Clamp => _clamp;
    }

    internal static class MotionUtility
    {
        internal const string DefaultMotionID = "Default";
        internal const float  MaxSubstepTime  = 1f / 60f;

        /// <summary>
        /// Получить Ease функцию
        /// </summary>
        /// <remarks>
        /// Больше примеров здесь: https://easings.net/
        /// </remarks>
        /// <param name="ease">Ease тип</param>
        /// <param name="t">Нормализованное время (0 - 1)</param>
        /// <returns>Возвращает значение</returns>
        internal static float GetEase(Ease ease, float t)
        {
            return ease switch
            {
                Ease.InSine => 1 - Mathf.Cos((t * Mathf.PI) / 2),
                Ease.OutSine => Mathf.Sin((t * Mathf.PI) / 2),
                Ease.InOutSine => -(Mathf.Cos(Mathf.PI * t) - 1) / 2,

                Ease.InQuad => t * t,
                Ease.OutQuad => 1 - (1 - t) * (1 - t),
                Ease.InOutQuad => t < 0.5 ? 2 * t * t : 1 - Mathf.Pow(-2 * t + 2, 2) / 2,

                Ease.InCubic => t * t * t,
                Ease.OutCubic => 1 - Mathf.Pow(1 - t, 3),
                Ease.InOutCubic => t < 0.5 ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) / 2,

                Ease.InQuart => t * t * t * t,
                Ease.OutQuart => 1 - Mathf.Pow(1 - t, 4),
                Ease.InOutQuart => t < 0.5 ? 8 * t * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 4) / 2,

                Ease.InQuint => t * t * t * t * t,
                Ease.OutQuint => 1 - Mathf.Pow(1 - t, 5),
                Ease.InOutQuint => t < 0.5 ? 16 * t * t * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 5) / 2,

                Ease.InExpo => t == 0 ? 0 : Mathf.Pow(2, 10 * t - 10),
                Ease.OutExpo => t == 1 ? 1 : 1 - Mathf.Pow(2, -10 * t),
                Ease.InOutExpo => t == 0 ? 0 : t == 1 ? 1 : t < 0.5 ? Mathf.Pow(2, 20 * t - 10) / 2 : (2 - Mathf.Pow(2, -20 * t + 10)) / 2,

                _ => t
            };
        }

        /// <summary>
        /// Получить физическое состояние
        /// </summary>
        /// <param name="current">Текущее состояние</param>
        /// <param name="deltaTime">Дельта времени</param>
        /// <param name="target">Целевое значение</param>
        /// <param name="setup">Настройка состояния</param>
        /// <returns>Возвращает MotionSpringState</returns>
        internal static void SetSpringState(
            ref MotionSpringState current,
            float deltaTime,
            float target,
            MotionStepSetupSpring setup)
        {
            // Сила пружины: F = -k * x
            float force = -setup.Stiffness * (current.Value - target);
            // Сила трения/затухания: F = -d * v
            float dampingForce = -setup.Damping * current.Velocity;

            // Ускорение (при массе = 1): a = F / m
            float acceleration = force + dampingForce;

            // Обновляем скорость и значение
            current.Velocity += acceleration * deltaTime;
            current.Value    += current.Velocity * deltaTime;
        }
    }
}
