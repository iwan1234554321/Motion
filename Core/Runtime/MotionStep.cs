using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Notteam.Motion
{
    /// <summary>
    /// Базовый шаг моушен компонента
    /// </summary>
    public abstract class MotionStepBase
    {
        private MotionKey _key;

        public MotionStepBase(MotionKey key)
        {
            _key = key;
        }

        public MotionKey Key => _key;
        public float     DelayTime        { get; protected set; }
        internal float   DelayTimeInverse { get; set; }
        internal bool    IsParallel       { get; set; }

        /// <summary>
        /// Задержка шага
        /// </summary>
        /// <param name="delay">Время задержки</param>
        /// <param name="unscaledTime">Флаг установки не масштабируемого времени обновления шага</param>
        /// <returns></returns>
        internal IEnumerator Delay(bool unscaledTime)
        {
            yield return Delay(DelayTime, unscaledTime);
        }

        internal IEnumerator Delay(float duration, bool unscaledTime)
        {
            if (duration > 0.0f)
            {
                if (unscaledTime)
                    yield return new WaitForSecondsRealtime(duration);
                else
                    yield return new WaitForSeconds(duration);
            }
        }

        /// <summary>
        /// Остановка шага
        /// </summary>
        /// <param name="toDefault">Флаг остановки шага на начальном значении</param>
        protected virtual void StopStep(bool toDefault = false) { }
        /// <summary>
        /// Завершить шаг
        /// </summary>
        protected virtual void CompleteStep() { }
        /// <summary>
        /// Задержать шаг
        /// </summary>
        protected virtual void HoldStep(bool inverse = false) { }

        /// <summary>
        /// Обновление шага
        /// </summary>
        /// <param name="inverse">Флаг инверсии обновления шага</param>
        /// <param name="unscaledTime">Флаг установки не масштабируемого времени обновления шага</param>
        /// <returns></returns>
        protected abstract IEnumerator UpdateStep(bool inverse, bool unscaledTime, bool hold = false);

        /// <summary>
        /// Остановка шага (применяется внутри сборки модуля Motion)
        /// </summary>
        /// <param name="toDefault">Флаг остановки шага на начальном значении</param>
        internal void Stop(bool toDefault = false)
        {
            StopStep(toDefault);
        }

        /// <summary>
        /// Завершение шага (применяется внутри сборки модуля Motion)
        /// </summary>
        internal void Complete()
        {
            CompleteStep();
        }

        /// <summary>
        /// Обновление шага (применяется внутри сборки модуля Motion)
        /// </summary>
        /// <param name="inverse">Флаг инверсии обновления шага</param>
        /// <param name="unscaledTime">Флаг установки не масштабируемого времени обновления шага</param>
        /// <returns>Возвращает IEnumerator</returns>
        internal IEnumerator Update(bool inverse, bool unscaledTime, bool hold = false)
        {
            yield return UpdateStep(inverse, unscaledTime, hold);
        }

        /// <summary>
        /// Удержания обновления шага в конечном значении
        /// </summary>
        internal void Hold(bool inverse = false)
        {
            HoldStep(inverse);
        }
    }

    /// <summary>
    /// Шаг моушен компонента
    /// </summary>
    public class MotionStep : MotionStepBase
    {
        private Action<float> _onUpdate;

        private MotionStepSetup _setup;

        public MotionStep(MotionKey key, Action<float> onUpdate, MotionStepSetup setup) : base(key)
        {
            _onUpdate = onUpdate;

            if (setup == null)
            {
                _setup = new MotionStepSetupTween();

                return;
            }

            _setup = setup;

            DelayTime = _setup.Delay;
        }

        /// <summary>
        /// Обновление промежуточной анимации шага
        /// </summary>
        /// <param name="setupTween">Настройки промежуточной анимации</param>
        /// <param name="inverse">Флаг инверсии обновления шага</param>
        /// <param name="unscaledTime">Флаг установки не масштабируемого времени обновления шага</param>
        /// <returns>Возвращает IEnumerator</returns>
        private IEnumerator UpdateTween(MotionStepSetupTween setupTween, bool inverse, bool unscaledTime)
        {
            var currentValue = inverse ? setupTween.Duration : 0.0f;

            var complete = false;

            while (!complete)
            {
                var normalized = currentValue / setupTween.Duration;

                var ease = MotionUtility.GetEase(setupTween.Ease, normalized);

                if (inverse)
                {
                    if (currentValue <= 0.0f)
                    {
                        currentValue = 0.0f;

                        complete = true;
                    }
                }
                else
                {
                    if (currentValue >= setupTween.Duration)
                    {
                        currentValue = normalized;

                        complete = true;
                    }
                }

                _onUpdate?.Invoke(ease);

                if (inverse)
                    currentValue -= unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                else
                    currentValue += unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

                yield return null;
            }

            _onUpdate?.Invoke(MotionUtility.GetEase(setupTween.Ease, currentValue));
        }

        /// <summary>
        /// Обновление физического шага анимации
        /// </summary>
        /// <param name="setupSpring">Настройка физики анимации</param>
        /// <param name="inverse">Флаг инверсии обновления шага</param>
        /// <param name="unscaledTime">Флаг установки не масштабируемого времени обновления шага</param>
        /// <returns>Возвращает IEnumerator</returns>
        private IEnumerator UpdateSpring(MotionStepSetupSpring setupSpring, bool inverse, bool unscaledTime)
        {
            var targetValue  = inverse ? 0.0f : 1.0f;
            var currentValue = inverse ? 1.0f : 0.0f;

            var currentVelocity = 0.0f;

            var complete = false;

            while (!complete)
            {
                var dt = unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

                dt = Mathf.Min(dt, 0.1f);

                var state = new MotionSpringState(currentValue, currentVelocity);

                // Цикл с субшагированием deltaTime для избежания взрыва физики
                while (dt > 0.0f)
                {
                    float step = Mathf.Min(dt, MotionUtility.MaxSubstepTime);

                    MotionUtility.SetSpringState(ref state, step, targetValue, setupSpring);

                    // bounce обрабатывается строго в момент пересечения границ шага
                    if (state.Value > 1.0f && setupSpring.Bounce > 0.0f)
                    {
                        state.Value    = 1.0f;
                        state.Velocity = -state.Velocity * setupSpring.Bounce;
                    }
                    else if (state.Value < 0.0f && setupSpring.Bounce > 0.0f)
                    {
                        state.Value    = 0.0f;
                        state.Velocity = -state.Velocity * setupSpring.Bounce;
                    }

                    dt -= step;
                }

                currentValue    = state.Value;
                currentVelocity = state.Velocity;

                if (setupSpring.Clamp && setupSpring.Bounce <= 0.0f)
                {
                    if (currentValue >= 1.0f || currentValue <= 0.0f)
                    {
                        currentValue = targetValue;

                        complete = true;
                    }
                }
                else
                {
                    if (Mathf.Abs(currentValue - targetValue) < 0.001f && Mathf.Abs(currentVelocity) < 0.01f)
                    {
                        currentValue = targetValue;

                        complete = true;
                    }
                }

                _onUpdate?.Invoke(currentValue);

                yield return null;
            }

            _onUpdate?.Invoke(currentValue);
        }

        protected override void StopStep(bool toDefault = false)
        {
            if (toDefault)
                _onUpdate?.Invoke(0.0f);
        }

        protected override void CompleteStep()
        {
            _onUpdate?.Invoke(1.0f);
        }

        protected override void HoldStep(bool inverse = false)
        {
            _onUpdate?.Invoke(inverse ? 0.0f : 1.0f);
        }

        /// <summary>
        /// Обновление шага
        /// </summary>
        /// <param name="inverse">Флаг инверсии обновления шага</param>
        /// <param name="unscaledTime">Флаг установки не масштабируемого времени обновления шага</param>
        /// <returns></returns>
        protected override IEnumerator UpdateStep(bool inverse, bool unscaledTime, bool hold = false)
        {
            if (!inverse)
                yield return Delay(unscaledTime);

            if (inverse && IsParallel)
                yield return Delay(DelayTimeInverse, unscaledTime);

            if (_setup != null)
            {
                if (_setup is MotionStepSetupTween setupTween)
                    yield return UpdateTween(setupTween, inverse, unscaledTime);
                else if (_setup is MotionStepSetupSpring setupSpring)
                    yield return UpdateSpring(setupSpring, inverse, unscaledTime);
            }

            if (inverse && !IsParallel)
                yield return Delay(unscaledTime);
        }
    }

    /// <summary>
    /// Группа параллельных шагов моушен компонента
    /// </summary>
    public class MotionStepParallel : MotionStepBase
    {
        private List<MotionStepBase> _group = new();

        private List<MotionStepBase> _holdSteps;

        private List<Coroutine> _groupCoroutines;

        public MotionStepParallel(MotionKey key) : base(key) { }

        private IEnumerator UpdateHoldSteps(bool inverse = false)
        {
            while (_holdSteps != null)
            {
                foreach (var step in _holdSteps)
                {
                    step.Hold(inverse);
                }

                yield return null;
            }
        }

        protected override void StopStep(bool toDefault = false)
        {
            _holdSteps = null;

            if (_group != null)
            {
                if (toDefault)
                {
                    for (var i = _group.Count - 1; i >= 0; i--)
                    {
                        var step = _group[i];

                        step.Stop(toDefault);
                    }
                }
            }

            if (_groupCoroutines != null)
            {
                for (var i = _groupCoroutines.Count - 1; i >= 0; i--)
                {
                    var coroutine = _groupCoroutines[i];

                    Key.Target.StopCoroutine(coroutine);
                }
            }
        }

        protected override void CompleteStep()
        {
            if (_group != null)
            {
                for (var i = _group.Count - 1; i >= 0; i--)
                {
                    var step = _group[i];

                    step.Complete();
                }
            }

            if (_groupCoroutines != null)
            {
                for (var i = _groupCoroutines.Count - 1; i >= 0; i--)
                {
                    var coroutine = _groupCoroutines[i];

                    Key.Target.StopCoroutine(coroutine);
                }
            }
        }

        protected override void HoldStep(bool inverse = false)
        {
            if (_group != null)
            {
                foreach (var step in _group)
                {
                    step.Hold(inverse);
                }
            }
        }

        protected override IEnumerator UpdateStep(bool inverse, bool unscaledTime, bool hold = false)
        {
            _groupCoroutines = new List<Coroutine>();

            var currentSteps = new List<MotionStepBase>();

            if (hold)
            {
                _holdSteps = new List<MotionStepBase>();

                if (inverse)
                    _holdSteps.AddRange(_group);

                Key.Target.StartCoroutine(UpdateHoldSteps());
            }

            var start     = !inverse ? 0 : _group.Count - 1;
            var direction = !inverse ? 1 : -1;

            for (var i = start; inverse ? i >= 0 : i < _group.Count; i += direction)
            {
                var step = _group[i];

                var coroutine = Key.Target.StartCoroutine(step.Update(inverse, unscaledTime));

                _groupCoroutines.Add(coroutine);

                if (hold)
                    currentSteps.Add(step);
            }

            for (int i = 0; i < _groupCoroutines.Count; i++)
            {
                var coroutine = _groupCoroutines[i];
                var groupStep = hold ? currentSteps[i] : null;

                if (inverse && hold)
                    _holdSteps.Remove(groupStep);

                yield return coroutine;

                if (!inverse && hold)
                    _holdSteps.Add(groupStep);
            }

            if (hold)
                _holdSteps = null;
        }

        internal void Add(MotionStepBase step)
        {
            step.IsParallel = true;

            var lastStep = _group.Count > 0 ? _group[_group.Count - 1] : null;

            if (lastStep != null)
            {
                lastStep.DelayTimeInverse = step.DelayTime / 2;
            }

            _group.Add(step);
        }
    }

    /// <summary>
    /// Шаг паузы моушен компонента
    /// </summary>
    public class MotionStepInterval : MotionStepBase
    {
        public MotionStepInterval(MotionKey key, float duration) : base(key)
        {
            DelayTime = duration;
        }

        protected override IEnumerator UpdateStep(bool inverse, bool unscaledTime, bool hold = false)
        {
            if (inverse)
                yield return null;
            else
                yield return Delay(unscaledTime);
        }
    }

    /// <summary>
    /// Шаг обратного вызова моушен компонента
    /// </summary>
    public class MotionStepCallback : MotionStepBase
    {
        private Action _callback;

        public MotionStepCallback(MotionKey key, Action callback) : base(key)
        {
            _callback = callback;
        }

        protected override IEnumerator UpdateStep(bool inverse, bool unscaledTime, bool hold = false)
        {
            if (!inverse)
                _callback?.Invoke();

            yield break;
        }
    }
}
