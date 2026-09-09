using UnityEngine;

namespace JohnStairs.RPG {
    public class EnableMaterialZWrite : MonoBehaviour {
        protected virtual void Start() {
            Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers) {
                foreach (Material material in renderer.materials) {
                    Utils.EnableZWrite(material);
                }
            }
        }
    }
}
