using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class ProBuilderItemsScript : MonoBehaviour
{
    // Members presented in editor
    public Material CubeMaterial;
    public Material SelectionMaterial;


    // Private members
    private ProBuilderMesh _mesh;


    /// <summary>
    /// Unity's Awake call
    /// </summary>
    private void Awake()
    {
        makeCube();
    }


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            var pickFace = SelectionPicker.PickFace(Camera.main, Input.mousePosition, _mesh);
            selectFace(pickFace);
        }
    }

    void OnGUI()
    {
        if (GUILayout.Button("Bevel"))
        {
            var selectedFaces = this.selectedFaces();
            foreach(var face in selectedFaces)
            {
                Bevel.BevelEdges(_mesh, face.edges, 0.3f);
            }
            if(selectedFaces.Count() > 0)
            {
                _mesh.ToMesh();
                _mesh.Refresh();
            }
        }
        if (GUILayout.Button("Clear selection"))
        {
            clearSelection();
        }
        if (GUILayout.Button("Reset"))
        {
            makeCube();
        }
    }


    private void makeCube()
    {
        if(_mesh != null)
        {
            Object.DestroyImmediate(_mesh.gameObject);
        }

        var generated = ShapeGenerator.GenerateCube(UnityEngine.ProBuilder.PivotLocation.Center, Vector3.one * 5);
        generated.transform.localPosition = new Vector3(0, 2.5f, 0);

        generated.GetComponent<MeshRenderer>().sharedMaterials = new Material[2]
        {
            CubeMaterial,
            SelectionMaterial
        };

        _mesh = generated;
    }

    private void selectFace(Face face)
    {
        if(face != null)
        {
            int currentIndex = face.submeshIndex;
            face.submeshIndex = currentIndex == 0 ? 1 : 0;
            _mesh.ToMesh();
            _mesh.Refresh();
        }
    }

    private IEnumerable<Face> selectedFaces()
    {
        return _mesh.faces.Where(f => f.submeshIndex == 1);
    }

    private void clearSelection()
    {
        bool hadSelected = false;
        foreach (var iFace in _mesh.faces)
        {
            hadSelected |= iFace.submeshIndex != 0;
            iFace.submeshIndex = 0;
        }
        if(hadSelected)
        {
            _mesh.ToMesh();
            _mesh.Refresh();
        }
    }
}
