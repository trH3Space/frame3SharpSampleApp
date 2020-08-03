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
        //UnityEngine.ProBuilder.Experimental.CSG.CSG.Union(new GameObject(), new GameObject());
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
        if (GUILayout.Button("Extrude faces"))
        {
            var selectedFaces = this.selectedFaces();
            if (selectedFaces.Count() > 0)
            {
                ExtrudeElements.Extrude(_mesh, selectedFaces, ExtrudeMethod.FaceNormal, 0.5f);
                _mesh.ToMesh();
                _mesh.Refresh();
            }
        }
        if (GUILayout.Button("Move face vertices"))
        {
            var selectedVertices = this.selectedVertices();
            if (selectedVertices.Count() > 0)
            {
                VertexPositioning.TranslateVertices(_mesh, selectedVertices, Vector3.right * 0.3f);
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
            clearSelection();
        }
    }


    private void makeCube()
    {
        if(_mesh != null)
        {
            Object.DestroyImmediate(_mesh.gameObject);
        }

        var generated = ShapeGenerator.GenerateCube(PivotLocation.Center, Vector3.one * 5);
        generated.transform.localPosition = new Vector3(0, 2.5f, 0);

        var renderer = generated.GetComponent<MeshRenderer>();
        renderer.sharedMaterials = new Material[2]
        {
            CubeMaterial,
            SelectionMaterial
        };

        _mesh = generated;
        _mesh.ToMesh();
        _mesh.Refresh();
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


    private IEnumerable<int> selectedVertices()
    {
        var selected = new HashSet<int>();
        foreach(var face in selectedFaces())
        {
            selected.UnionWith(face.indexes);
        }
        return selected;
    }


    private void clearSelection()
    {
        foreach (var iFace in _mesh.faces)
        {
            iFace.submeshIndex = 0;
        }
        _mesh.ToMesh();
        _mesh.Refresh();
    }
}
