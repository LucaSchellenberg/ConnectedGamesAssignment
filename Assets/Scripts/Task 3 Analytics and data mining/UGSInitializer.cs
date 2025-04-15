using Unity.Services.Core;
using Unity.Services.Analytics;
using UnityEngine;
using System.Threading.Tasks;

public class UGSInitializer : MonoBehaviour
{
    async void Start()
    {
        await UnityServices.InitializeAsync();
        // await AuthenticationService.Instance.SignInAnonymouslyAsync();
        Debug.Log("Unity Gaming Services Initialized");
    }
}
