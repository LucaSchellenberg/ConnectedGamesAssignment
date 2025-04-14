
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;

public class ItemPurchase : MonoBehaviour
{
    public StoreItem Item;
    public void DownloadItem()
    {
        if (StoreManager.Instance.PurchaseItem(Item.Price))
        {
            GetComponent<Button>().enabled = false;
            GetComponent<Animator>().SetTrigger("Disabled");
            string internalUrl = Item.ThumbnailUrl.Split("firebasestorage.app/")[1];
            string filename = Item.Name.Replace(" ", "");
            string filepath = Path.Combine(UnityEngine.Application.persistentDataPath, filename + "." + internalUrl.Split(".")[1]);
            FirebaseStorageManager.Instance.DownloadToFile(internalUrl, filepath);
        }
        else
        {
            UnityEngine.Debug.LogWarning("Insuffient balance!!");
        }

    }
}