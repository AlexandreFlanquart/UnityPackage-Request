using UnityEngine;

public class LoadingUI : MonoBehaviour
{
    private int nbPoint = 7;
    [SerializeField]bool isUpdate = false;
    [SerializeField] GameObject[] points;
    [SerializeField] GameObject pointPrefab;
    private float rayon = 10f;
    private float scale = 0.05f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        points = new GameObject[nbPoint];
        for(int i = 0; i < nbPoint; i++)
        {
            points[i] = Instantiate(pointPrefab, transform);
            points[i].transform.SetParent(transform);
            points[i].transform.localScale = Vector3.one * scale;
            float x = Mathf.Cos(Mathf.PI * 2 * i / nbPoint) * rayon;
            float y = Mathf.Sin(Mathf.PI * 2 * i / nbPoint) * rayon;
            points[i].transform.localPosition = new Vector3(x, y, 0);
            points[i].SetActive(true);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(isUpdate)
        {
            transform.Rotate(Vector3.forward, 1f);
        }
    }
}
