using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using UnityChess;



public class ItemPurchase : MonoBehaviour
{
    public StoreItem Item;

    public void DownloadItem()
    {
        // First, check if the player can purchase the item.
        if (StoreManager.Instance.PurchaseItem(Item.Price))
        {
            // Disable the purchase button to prevent multiple clicks.
            GetComponent<Button>().enabled = false;
            // Optionally trigger an animation:
            // GetComponent<Animator>()?.SetTrigger("Disabled");

            // Parse the internal URL from ThumbnailUrl.
            // (Assuming the URL contains "firebasestorage.app/" and a file extension, e.g., ".png")
            string internalUrl = Item.ThumbnailUrl.Split("firebasestorage.app/")[1];
            string filename = Item.Name.Replace(" ", "");
            string extension = internalUrl.Split('.')[1]; // For example "png"
            // Construct the filepath where the asset will be saved.
            string filepath = Path.Combine(Application.persistentDataPath, filename + "." + extension);

            // Start the download.
            FirebaseStorageManager.Instance.DownloadToFile(internalUrl, filepath);

            // Start a coroutine that waits for the file and then applies the skin.
            StartCoroutine(LoadAndApplySkin(filepath));
        }
        else
        {
            Debug.LogWarning("Insufficient balance!!");
        }
    }

    IEnumerator LoadAndApplySkin(string filepath)
    {
        // Wait until the file exists (with a timeout if needed)
        float timer = 0f;
        while (!File.Exists(filepath) && timer < 10f)
        {
            timer += Time.deltaTime;
            yield return null;
        }
        if (!File.Exists(filepath))
        {
            Debug.LogError("Skin file not found at: " + filepath);
            yield break;
        }

        byte[] fileData = null;
        bool fileReady = false;
        while (!fileReady)
        {
            bool exceptionCaught = false;
            try
            {
                using (FileStream fs = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    fileData = new byte[fs.Length];
                    fs.Read(fileData, 0, (int)fs.Length);
                }
            }
            catch (IOException ex)
            {
                Debug.Log("File is locked. Waiting a bit before retrying... " + ex.Message);
                exceptionCaught = true;
            }

            if (exceptionCaught)
            {
                // Yield outside of the catch block.
                yield return new WaitForSeconds(0.1f);
            }
            else
            {
                fileReady = true;
            }
        }

        Texture2D newSkinTexture = new Texture2D(2, 2);
        newSkinTexture.LoadImage(fileData);

        // Determine the target side (adjust if your logic is different)
        Side targetSide = (Unity.Netcode.NetworkManager.Singleton.LocalClientId == 0) ? Side.White : Side.Black;

        GameManager.Instance.ApplySkinToPieces(newSkinTexture, targetSide);
        yield return null;
    }


}
