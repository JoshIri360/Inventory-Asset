using InventorySystem;
using UnityEngine;

internal class FashionApplicator : MonoBehaviour
{
    [SerializeField] GameObject hatFlippedFalse;
    [SerializeField] GameObject hatFlippedTrue;

    [SerializeField] Sprite turqoiseHat;
    [SerializeField] Sprite boringHat;
    [SerializeField] Sprite redHat;
    [SerializeField] GameObject redHatobj;
    SpriteRenderer srHatFlippedFalse;
    SpriteRenderer srHatFlippedTrue;

    private void Start()
    {
        srHatFlippedFalse= hatFlippedFalse.GetComponent<SpriteRenderer>();
        srHatFlippedTrue = hatFlippedTrue.GetComponent<SpriteRenderer>();
    }
    public void SetTurqoiseHat()
    {
        srHatFlippedFalse.sprite = turqoiseHat;
        srHatFlippedTrue.sprite= turqoiseHat;
    }
    public void SetBoringHat()
    {
        srHatFlippedFalse.sprite = boringHat;
        srHatFlippedTrue.sprite = boringHat;
    }
    public void SetRedHat()
    {
        srHatFlippedFalse.sprite = redHat;
        srHatFlippedTrue.sprite = redHat;
    }
    public void DropItem(Vector3 pos, InventoryItem item)
    {
        for(int i = 0; i < item.GetAmount(); i++) {
            Instantiate(item.GetRelatedGameObject(), pos, Quaternion.identity);
        }
    }
}
