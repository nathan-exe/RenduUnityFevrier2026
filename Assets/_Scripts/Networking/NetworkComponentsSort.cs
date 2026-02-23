using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


/// <summary>
/// les behaviours donnés ne seront activés que sur l'owner.
/// </summary>
public class NetworkComponentsSort : NetworkBehaviour
{
    [SerializeField] List<Behaviour> _ownerOnlyBehaviours;
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsOwner)
        {
            foreach (Behaviour component in _ownerOnlyBehaviours)
            {
                component.enabled = false;
            }
        }
        Destroy(this);
    }
}
