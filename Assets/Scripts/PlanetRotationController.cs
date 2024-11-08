using UnityEngine;

public class PlanetRotationController : MonoBehaviour
{
    //[SerializeField] private Material earthMaterial;

    //private void LateUpdate()
    //{
    //    earthMaterial.SetFloat("TextureOffset", (float)(earthMaterial.GetFloat("TextureOffset") + 0.1 * Time.deltaTime));
    //}

    //public void Start()
    //{
    //    Material material = GetComponent<Renderer>().material;
    //    material.SetFloat("TextureOffset", 0f);
    //}

    public void SetRotation()
    {
        Material material = GetComponent<Renderer>().material;
        material.SetFloat("_TextureOffset", (float) (material.GetFloat("_TextureOffset") + 0.3 * Time.deltaTime));
        Debug.Log(material.GetFloat("_TextureOffset"));
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.Space))
        {
            SetRotation();
        }
    }
}
