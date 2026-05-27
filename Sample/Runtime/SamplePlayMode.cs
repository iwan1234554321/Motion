using System.Collections;
using UnityEngine;

namespace Notteam.Motion
{
    public class SamplePlayMode : MonoBehaviour
    {
        [SerializeField] private bool  switchFramerate;
        [SerializeField] private bool  useCustomFrameRate;
        [SerializeField] private int   customFrameRate = 60;
        [Space]
        [SerializeField] private bool              animate;
        [SerializeField] private bool              stopAnimate;
        [SerializeField] private bool              stopToDefault;
        [Space]
        [SerializeField] private bool              useSpring;
        [Header("Setup")]
        [SerializeField] private MotionStepSetupTween  setupTween;
        [SerializeField] private MotionStepSetupTween  setupTween2;
        [SerializeField] private MotionStepSetupSpring setupSpring;
        [SerializeField] private MotionStepSetupSpring setupSpring2;
        [Space]
        [SerializeField] private Playback playback;
        [SerializeField] private bool     unscaledTime;
        [SerializeField] private float    delay = 2;
        
        [Header("Move")]
        [SerializeField] private Vector3 startPoint;
        [SerializeField] private Vector3 midPoint;
        [SerializeField] private Vector3 finalPoint;

        [Header("Reference")]
        [SerializeField] private Transform transformObject;

        private IEnumerator PlayCoroutine()
        {
            yield return this.Motion().
                    OnStart(() => { Debug.Log("Start Coroutine Animation"); }).
                    AddStepInterval(delay).
                    AddStepParallel((t) => {
                        transformObject.rotation = Quaternion.LerpUnclamped(Quaternion.identity, Quaternion.AngleAxis(45, Vector3.forward), t);
                        transformObject.position = Vector3.LerpUnclamped(startPoint, finalPoint, t);
                    },
                    !useSpring ? setupTween : setupSpring).
                    AddStepParallel((t) =>
                    {
                        transformObject.localScale = Vector3.LerpUnclamped(Vector3.one, Vector3.one * 1.2f, t);
                    },
                    !useSpring ? setupTween2 : setupSpring2).
                    Play(playback, unscaledTime).
                    OnComplete(() => { Debug.Log("Stop Coroutine Animation"); }).
                    Wait();
        }

        private async void PlayAsync()
        {
            await this.Motion().
                    OnStart(() => { Debug.Log("Start Async Animation"); }).
                    AddStepInterval(delay).
                    AddStepParallel((t) => {
                        transformObject.rotation = Quaternion.LerpUnclamped(Quaternion.identity, Quaternion.AngleAxis(45, Vector3.forward), t);
                        transformObject.position = Vector3.LerpUnclamped(startPoint, finalPoint, t);
                    },
                    !useSpring ? setupTween : setupSpring).
                    AddStepParallel((t) =>
                    {
                        transformObject.localScale = Vector3.LerpUnclamped(Vector3.one, Vector3.one * 1.2f, t);
                    },
                    !useSpring ? setupTween2 : setupSpring2).
                    Play(playback, unscaledTime).
                    OnComplete(() => { Debug.Log("Stop Async Animation"); }).
                    Await();
        }

        private void Update()
        {
            if (switchFramerate)
            {
                if (useCustomFrameRate)
                {
                    Application.targetFrameRate = customFrameRate;
                }
                else
                {
                    Application.targetFrameRate = -1;
                }

                switchFramerate = false;
            }

            if (animate)
            {
                StartCoroutine(PlayCoroutine());

                //PlayAsync();

                animate = false;
            }

            if (stopAnimate)
            {
                this.StopMotion(toDefault: stopToDefault);

                stopAnimate = false;
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(startPoint, new Vector3(1, 0.1f, 1));

            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(midPoint, new Vector3(1, 0.1f, 1));

            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(finalPoint, new Vector3(1, 0.1f, 1));
        }
    }
}
