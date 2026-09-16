using UnityEngine;

namespace GrowNa.Pet
{
    public class PetView : MonoBehaviour
    {
        [SerializeField] SpriteRenderer body;
        PetService service;

        public void Bind(SpriteRenderer renderer) => body = renderer;

        public void BindService(PetService bound)
        {
            if (service != null) service.Changed -= Refresh;
            service = bound;
            if (service != null) service.Changed += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (service != null) service.Changed -= Refresh;
        }

        void Refresh()
        {
            if (body == null || service == null) return;

            bool has = service.HasPet;
            body.enabled = has;
            if (has) body.color = PetTable.Color(PetTable.Get(service.ActiveId).rarity);
        }
    }
}
