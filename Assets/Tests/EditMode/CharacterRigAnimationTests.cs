using GrowNa.Gear;
using GrowNa.Players;
using NUnit.Framework;
using UnityEngine;

namespace GrowNa.Tests
{
    public class CharacterRigAnimationTests
    {
        GameObject root;
        Texture2D texture;

        [TearDown]
        public void TearDown()
        {
            if (root != null) Object.DestroyImmediate(root);
            if (texture != null) Object.DestroyImmediate(texture);
        }

        [Test]
        public void Equipped_layer_follows_idle_run_and_attack_frames()
        {
            root = new GameObject("Rig");
            texture = new Texture2D(4, 4);
            Sprite Make() => Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);

            var body = root.AddComponent<SpriteRenderer>();
            body.sprite = Make();
            var layers = new SpriteRenderer[GearTable.SlotCount];
            var defaults = new Sprite[GearTable.SlotCount];
            var animations = new CharacterRig.AnimatedSlot[GearTable.SlotCount];
            layers[(int)GearSlot.Weapon] = new GameObject("Weapon").AddComponent<SpriteRenderer>();
            layers[(int)GearSlot.Weapon].transform.SetParent(root.transform, false);
            defaults[(int)GearSlot.Weapon] = Make();
            var idle = Make();
            var run = Make();
            var attack = Make();
            animations[(int)GearSlot.Weapon] = new CharacterRig.AnimatedSlot
            {
                idle = new[] { idle },
                run = new[] { run },
                attack = new[] { attack },
            };

            var rig = root.AddComponent<CharacterRig>();
            rig.Bind(body, layers, defaults, animations);
            rig.Apply(GearSlot.Weapon, GearItem.Of(GearSlot.Weapon, GearTier.Rare));
            Assert.AreSame(idle, layers[(int)GearSlot.Weapon].sprite);

            rig.SetAnimationFrame(PlayerAnimationClip.Run, 0);
            Assert.AreSame(run, layers[(int)GearSlot.Weapon].sprite);

            rig.SetAnimationFrame(PlayerAnimationClip.Attack, 0);
            Assert.AreSame(attack, layers[(int)GearSlot.Weapon].sprite);
        }
    }
}
