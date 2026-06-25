using UnityEngine;
using UnityEngine.InputSystem;

namespace MemoryColoseum.Combat
{
    public sealed class MixamoAnimationInputDriver : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private string idleState = "Idle";
        [SerializeField] private string punchState = "Punch";
        [SerializeField] private string kickState = "Kick";
        [SerializeField] private string victoryState = "Victory";
        [SerializeField] private string defeatState = "Defeat";
        [SerializeField, Min(0f)] private float fadeDuration = 0.08f;

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || animator == null)
            {
                return;
            }

            if (keyboard.digit1Key.wasPressedThisFrame)
            {
                Play(idleState);
            }
            else if (keyboard.digit2Key.wasPressedThisFrame)
            {
                Play(punchState);
            }
            else if (keyboard.digit3Key.wasPressedThisFrame)
            {
                Play(kickState);
            }
            else if (keyboard.digit4Key.wasPressedThisFrame)
            {
                Play(victoryState);
            }
            else if (keyboard.digit5Key.wasPressedThisFrame)
            {
                Play(defeatState);
            }
        }

        private void Play(string stateName)
        {
            animator.CrossFadeInFixedTime(stateName, fadeDuration);
        }
    }
}
