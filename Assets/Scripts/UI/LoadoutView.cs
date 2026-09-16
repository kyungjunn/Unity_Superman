using GrowNa.Gear;
using UnityEngine;
using UnityEngine.UI;

namespace GrowNa.UI
{
    public class LoadoutView : MonoBehaviour
    {
        [SerializeField] Image[] slotFrames = new Image[GearTable.SlotCount];
        [SerializeField] Text[] slotLabels = new Text[GearTable.SlotCount];

        Loadout loadout;

        static readonly Color EmptyTint = new Color(0.55f, 0.52f, 0.60f);

        public void Bind(Image[] frames, Text[] labels)
        {
            slotFrames = frames;
            slotLabels = labels;
        }

        public void BindServices(Loadout bound)
        {
            if (loadout != null) loadout.SlotChanged -= Apply;
            loadout = bound;
            if (loadout == null) return;
            loadout.SlotChanged += Apply;
            for (int i = 0; i < GearTable.SlotCount; i++)
                Apply((GearSlot)i, loadout.Get((GearSlot)i));
        }

        void OnDestroy()
        {
            if (loadout != null) loadout.SlotChanged -= Apply;
        }

        void Apply(GearSlot slot, GearItem item)
        {
            int index = (int)slot;
            if (index >= slotFrames.Length) return;

            if (slotFrames[index] != null)
                slotFrames[index].color = item.owned ? GearTable.Color(item.tier) : Color.white;

            if (slotLabels[index] != null)
            {
                slotLabels[index].text = item.owned
                    ? $"{GearTable.Name(slot)}\n{item.ShortLabel}"
                    : GearTable.Name(slot);
                slotLabels[index].color = item.owned ? GearTable.Color(item.tier) : EmptyTint;
            }
        }
    }
}
