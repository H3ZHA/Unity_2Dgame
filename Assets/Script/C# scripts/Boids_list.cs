using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boids_list : MonoBehaviour
{
    private List<GameObject> boids_L = new List<GameObject>();
    private int index = 0;

    // density of boids, if boids closer than this distance will avoid each other
    public float separation_distance;

    // size of boids, boids will follow the direction of other boids in this distance
    public float alignment_distance;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public List<GameObject> get_list(){
        return boids_L;
    }

    public void add(GameObject boid){
        boids_L.Add(boid);
        boid.GetComponent<Boids>().set_index(index);
        index++;

        // update all boids
        for(int i=0; i < boids_L.ToArray().Length; i++){
            boids_L[i].GetComponent<Boids>().get_boids_list();
        }
    }

    public void remove(int remove_index){
        int index = boids_L.FindIndex(t => t.GetComponent<Boids>().get_index() == remove_index);
        boids_L.RemoveAt(index);
    }
}
