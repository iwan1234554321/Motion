using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Notteam.Motion
{
    /// <summary>
    /// Моушен компонент
    /// </summary>
    public class MotionComponent
    {
        private MotionKey _key;
        private bool      _replace;

        private Playback  _playback;
        private bool      _unscaledTime;

        private Coroutine _coroutine;

        private List<MotionStepBase> _steps = new();

        private Action onStart;
        private Action onComplete;

        internal MotionComponent(MotionKey key, bool replace)
        {
            _key     = key;
            _replace = replace;
        }

        internal MotionKey Key     => _key;
        internal bool      Replace => _replace;

        /// <summary>
        /// Функция завершения моушен компонента если приложение завершено
        /// </summary>
        private void Quitting()
        {
            Stop();
        }

        /// <summary>
        /// Внутренняя остановка моушен компонента и отключение её от менеджера моушен компонентов
        /// </summary>
        private void StopInternal()
        {
            if (_coroutine != null)
            {
                if (_key.Target != null)
                {
                    _key.Target.StopCoroutine(_coroutine);
                }

                MotionManager.UnregisterMotion(this);

                _coroutine = null;
            }

            Application.quitting -= Quitting;

            onComplete?.Invoke();
        }

        /// <summary>
        /// Обновление цикла анимации моушена
        /// </summary>
        /// <param name="unscaledTime">Установка не масштабируемого времени</param>
        /// <returns>Возвращает перечислитель IEnumerator</returns>
        private IEnumerator Update(bool unscaledTime)
        {
            for (var i = _steps.Count - 1; i >= 0; i--)
            {
                var step = _steps[i];

                step.Stop(true);
            }

            var cycle   = true;
            var inverse = false;

            while (cycle)
            {
                var start     = !inverse ? 0 : _steps.Count - 1;
                var direction = !inverse ? 1 : -1;

                var hold = _playback == Playback.Hold || _playback == Playback.PingPong;

                for (var i = start; inverse ? i >= 0 : i < _steps.Count; i += direction)
                {
                    var step = _steps[i];

                    yield return step.Update(inverse, unscaledTime, hold);
                }

                if (_playback == Playback.Once)
                {
                    cycle = false;
                }
                else if (_playback == Playback.Loop)
                {
                    for (var i = _steps.Count - 1; i >= 0; i--)
                    {
                        var step = _steps[i];

                        step.Stop(true);
                    }
                }
                else if (_playback == Playback.PingPong)
                {
                    inverse = !inverse;
                }
                else if (_playback == Playback.Hold)
                {
                    while (true)
                    {
                        _steps[inverse ? 0 : _steps.Count - 1].Hold(inverse);

                        yield return null;
                    }
                }
            }

            Stop();
        }

        /// <summary>
        /// Завершение моушен компонента
        /// </summary>
        /// <param name="toDefault">Установка начального значения</param>
        public void Stop(bool toDefault = false)
        {
            for (var i = _steps.Count - 1; i >= 0; i--)
            {
                var step = _steps[i];

                step.Stop(toDefault);
            }

            StopInternal();
        }

        /// <summary>
        /// Завершение моушен компонента и установка его конечного значения
        /// </summary>
        /// <remarks>
        /// Анимация останавливается в конечной точке.
        /// </remarks>
        public void Complete()
        {
            for (var i = _steps.Count - 1; i >= 0; i--)
            {
                var step = _steps[i];

                step.Complete();
            }

            StopInternal();
        }

        /// <summary>
        /// Замена моушен компонента при перезаписи
        /// </summary>
        /// <param name="motion">Моушен компонент</param>
        internal void ReplaceMotion(MotionComponent motion)
        {
            for (var i = _steps.Count - 1; i >= 0; i--)
            {
                var step = _steps[i];

                step.Stop(true);
            }

            if (_coroutine != null && _key.Target != null)
                _key.Target.StopCoroutine(_coroutine);

            _key      = motion._key;
            _playback = motion._playback;

            _unscaledTime = motion._unscaledTime;

            _steps = motion._steps;
        }

        /// <summary>
        /// Воспроизведение моушен компонента
        /// </summary>
        /// <param name="playback">Тип воспроизведения</param>
        /// <param name="unscaledTime">Установка не масштабируемого времени</param>
        /// <returns>Возвращает моушен компонент</returns>
        public MotionComponent Play(Playback playback = Playback.Once, bool unscaledTime = false)
        {
            if (_key.Target == null || !_key.Target.gameObject.activeSelf || !_key.Target.gameObject.activeInHierarchy)
                return this;

            onStart?.Invoke();

            Application.quitting += Quitting;

            _playback     = playback;
            _unscaledTime = unscaledTime;

            MotionManager.RegisterMotion(this);

            _coroutine = _key.Target.StartCoroutine(Update(_unscaledTime));

            return this;
        }

        /// <summary>
        /// Функция для работы моушен компонента в корутинах
        /// </summary>
        /// <returns>Возвращает IEnumerator</returns>
        public IEnumerator Wait()
        {
            yield return _coroutine;
        }

        /// <summary>
        /// Функция для работы моушена внутри асинхронных функций
        /// </summary>
        /// <returns>Возвращает Task</returns>
        public async Task Await()
        {
            while (_coroutine != null && _key.Target != null && this != null)
            {
                await Task.Yield();
            }
        }

        /// <summary>
        /// Добавляет шаг анимации в моушен компонент
        /// </summary>
        /// <param name="onUpdate">Обратная функция где происходит обновление анимации моушен компонента</param>
        /// <param name="setup">Настройки моушен компонента</param>
        /// <returns>Возвращает моушен компонент</returns>
        public MotionComponent AddStep(Action<float> onUpdate, MotionStepSetup setup = null)
        {
            var step = new MotionStep(_key, onUpdate, setup);

            _steps.Add(step);

            return this;
        }

        /// <summary>
        /// Добавляет параллельный шаг анимации для моушен компонента
        /// </summary>
        /// <param name="onUpdate">Обратная функция где происходит обновление анимации моушен компонента</param>
        /// <param name="setup">Настройки моушен компонента</param>
        /// <returns>Возвращает моушен компонент</returns>
        public MotionComponent AddStepParallel(Action<float> onUpdate, MotionStepSetup setup = null)
        {
            void AddNewGroup(MotionStep step)
            {
                var group = new MotionStepParallel(_key);

                group.Add(step);

                _steps.Add(group);
            }

            var step = new MotionStep(_key, onUpdate, setup);

            var lastStep = _steps.Count > 0 ? _steps[_steps.Count - 1] : null;

            if (lastStep != null)
            {
                if (lastStep is MotionStepParallel group)
                    group.Add(step);
                else
                    AddNewGroup(step);
            }
            else
                AddNewGroup(step);

            return this;
        }

        /// <summary>
        /// Добавляет паузу в моушен компонент
        /// </summary>
        /// <param name="duration">Время паузы</param>
        /// <returns>Возвращает моушен компонент</returns>
        public MotionComponent AddStepInterval(float duration)
        {
            var step = new MotionStepInterval(_key, duration);

            _steps.Add(step);

            return this;
        }

        /// <summary>
        /// Добавляет шаг обратного вызова в моушен компонент 
        /// </summary>
        /// <param name="callback">Обратный вызов</param>
        /// <returns>Возвращает моушен компонент</returns>
        public MotionComponent AddStepCallback(Action callback)
        {
            var step = new MotionStepCallback(_key, callback);

            _steps.Add(step);

            return this;
        }

        /// <summary>
        /// Обратный вызов начала моушен компонента
        /// </summary>
        /// <param name="callback">Обратный вызов</param>
        /// <returns>Возвращает моушен компонент</returns>
        public MotionComponent OnStart(Action callback)
        {
            onStart = callback;

            return this;
        }

        /// <summary>
        /// Обратный вызов завершения моушен компонента
        /// </summary>
        /// <param name="callback">Обратный вызов</param>
        /// <returns>Возвращает моушен компонент</returns>
        public MotionComponent OnComplete(Action callback)
        {
            onComplete = callback;

            return this;
        }
    }
}
