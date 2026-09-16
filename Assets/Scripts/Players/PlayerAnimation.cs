using UnityEngine;

namespace GrowNa.Players
{
    public enum PlayerAnimationClip
    {
        Idle,
        Run,
        Attack,
    }

    public class PlayerAnimation : MonoBehaviour
    {
        [SerializeField] SpriteRenderer body;
        [SerializeField] Sprite[] idle;
        [SerializeField] Sprite[] run;
        [SerializeField] Sprite[] attack;
        [SerializeField] float idleFps = 8f;
        [SerializeField] float runFps = 12f;
        [SerializeField] float attackFps = 14f;
        [SerializeField] CharacterRig gearRig;

        bool running;
        bool punching;
        float elapsed;
        int index;

        public void Bind(SpriteRenderer renderer, Sprite[] idleFrames, Sprite[] runFrames, Sprite[] attackFrames)
        {
            body = renderer;
            idle = idleFrames;
            run = runFrames;
            attack = attackFrames;
            punching = false;
            running = false;
            elapsed = 0f;
            index = 0;
            Apply(CurrentClip());
        }

        public void SetRunning(bool value)
        {
            if (running == value) return;
            running = value;
            if (punching) return;
            elapsed = 0f;
            index = 0;
            Apply(CurrentClip());
        }

        public void BindGearRig(CharacterRig rig)
        {
            gearRig = rig;
            gearRig?.SetAnimationFrame(CurrentState(), index);
        }

        public void PlayAttack()
        {
            if (attack == null || attack.Length == 0) return;
            punching = true;
            elapsed = 0f;
            index = 0;
            Apply(attack);
        }

        void Awake()
        {
            if (body == null) body = GetComponent<SpriteRenderer>();
        }

        void Update()
        {
            Sprite[] clip = punching ? attack : CurrentClip();
            if (body == null || clip == null || clip.Length == 0) return;

            float fps = punching ? attackFps : running ? runFps : idleFps;
            elapsed += Time.deltaTime * fps;
            if (punching && elapsed >= clip.Length)
            {
                punching = false;
                elapsed = 0f;
                index = 0;
                clip = CurrentClip();
                Apply(clip);
                return;
            }

            int next = (int)elapsed % clip.Length;
            if (next == index) return;
            index = next;
            ApplyFrame(clip);
        }

        Sprite[] CurrentClip() => running ? run : idle;

        void Apply(Sprite[] clip)
        {
            if (body == null || clip == null || clip.Length == 0) return;
            index = 0;
            ApplyFrame(clip);
        }

        void ApplyFrame(Sprite[] clip)
        {
            body.sprite = clip[index];
            gearRig?.SetAnimationFrame(CurrentState(), index);
        }

        PlayerAnimationClip CurrentState()
            => punching ? PlayerAnimationClip.Attack
                        : running ? PlayerAnimationClip.Run
                                  : PlayerAnimationClip.Idle;
    }
}
