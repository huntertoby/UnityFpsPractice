using TMPro;
using UnityEngine;
using Image = UnityEngine.UI.Image;


public class BuyWeaponButton : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private Texture2D weaponImage;
    [SerializeField] private Image _image;
    [SerializeField] private TextMeshProUGUI weaponNameUI;
    [SerializeField] private TextMeshProUGUI weaponMoneyUI;
    [SerializeField] private string weaponName;
    [SerializeField] public int weaponMoney;
    [SerializeField] public int weaponIndex;



    void Start()
    {
        // 將 Texture2D 轉換為 Sprite
        Sprite weaponSprite = Sprite.Create(
            weaponImage,
            new Rect(0.0f, 0.0f, weaponImage.width, weaponImage.height),
            new Vector2(0.5f, 0.5f)
        );

        // 將轉換後的 Sprite 賦值給 Image 組件
        _image.sprite = weaponSprite;

        weaponNameUI.text = weaponName;
        weaponMoneyUI.text = "$" + weaponMoney;
    }
}
