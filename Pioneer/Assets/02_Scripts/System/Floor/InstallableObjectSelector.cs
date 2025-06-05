using UnityEngine;

public class InstallableObjectSelector : MonoBehaviour
{
    public InstallableChecker installableChecker;

    public void SetInstallable(SInstallableObjectDataSO data)
    {
        installableChecker.SetCurrentInstallableObject(data);
    }
}
