using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Creates a managed component to reference UI elements
/// </summary>
public partial struct UIInitSystem : ISystem
{
    public void OnCreate(ref SystemState state) {
        state.RequireForUpdate<Spawner>();
    }

    public void OnUpdate(ref SystemState state) {
        // run only once
        state.Enabled = false;

        // Get reference to UI Manager mono behavior
        var uiManager = GameObject.FindObjectOfType<UIManager>();

        // Create new managed component
        var uiData = new UIData();

        // Link mono behavior field (infoText) to managed component field
        uiData.infoText = uiManager.infoText;
        
        // Create a new entity and attach the managed component
        var entity = state.EntityManager.CreateEntity();
        state.EntityManager.AddComponentData(entity, uiData);
    }
}

public class UIData : IComponentData
{
    public Text infoText;

    // Every IComponentData class must have a no-arg constructor.
    public UIData() {
    }
}
