using System;
using UnityEngine;

namespace Notteam.Motion
{
    /// <summary>
    /// Базовый класс настройки моушен шага
    /// </summary>
    [Serializable]
    public abstract class MotionStepSetup
    {
        [SerializeField] private float _delay;

        public MotionStepSetup(float delay)
        {
            _delay = delay;
        }

        internal float Delay => _delay;
    }

    /// <summary>
    /// Настройка промежуточной анимации моушен шага
    /// </summary>
    [Serializable]
    public class MotionStepSetupTween : MotionStepSetup
    {
        [SerializeField] private float _duration;
        [SerializeField] private Ease  _ease;

        public MotionStepSetupTween(
            float duration = 1,
            Ease ease = Ease.Linear,
            float delay = 0.0f) : base(delay)
        {
            _duration = duration;
            _ease     = ease;
        }

        public float Duration => _duration;
        public Ease  Ease     => _ease;
    }

    /// <summary>
    /// Настройка физической анимации моушен шага
    /// </summary>
    [Serializable]
    public class MotionStepSetupSpring : MotionStepSetup
    {
        [SerializeField] private float _stiffness;
        [SerializeField] private float _damping;
        [SerializeField] private float _bounce;

        [SerializeField] private bool _clamp;

        public MotionStepSetupSpring(
            float stiffness = 150.0f,
            float damping = 10.0f,
            float bounce = 0.0f,
            bool clamp = false,
            float delay = 0.0f) : base(delay)
        {
            _stiffness = stiffness;
            _damping   = damping;
            _bounce    = bounce;

            _clamp = clamp;
        }

        public float Stiffness => _stiffness;
        public float Damping   => _damping;
        public float Bounce    => _bounce;

        public bool Clamp => _clamp;
    }
}
