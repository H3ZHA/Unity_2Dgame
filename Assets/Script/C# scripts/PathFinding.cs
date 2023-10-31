using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Unity.VisualScripting;

public class PathFinding : MonoBehaviour
{
    // object and location of target
    public GameObject target;

    // collision box of target
    private BoxCollider2D target_box;

    // referece to the BoxCollider2D
    private BoxCollider2D self_box;
    private float box_x;
    private float box_y;
    private float offset_x;
    private float offset_y;

    // Node of A*
    class Node{
        public Vector2 position;
        public float move_cost;
        public float total_cost;
        public Node parent;
        public Vector2 direction;

        public Node(Vector2 position, Vector2 target){
            this.position = position;
            this.move_cost = 0;
            this.total_cost = (target - position).magnitude;
            this.parent = null;
        }

        public Node(Vector2 position, Vector2 target, float move_cost, Node parent, Vector2 direction, float weight){
            this.position = position;
            this.move_cost = move_cost + parent.move_cost;
            this.total_cost = (target - position).magnitude * weight + this.move_cost;
            this.parent = parent;
            this.direction = direction;
        }
    }

    // path for A* result
    private List<Node> path = new List<Node>();

    // next node to move
    Node next_node = null;

    // Start is called before the first frame update
    void Start()
    {
        // Get and store a reference to the Rigidbody2D component 
        // so that we can access it. 
        self_box = GetComponent<BoxCollider2D>();
        box_x = self_box.size[0] * transform.lossyScale.x;
        box_y = self_box.size[1] * transform.lossyScale.y;
        offset_x = self_box.offset[0];
        offset_y = self_box.offset[1];

        // get player collision box
        target_box = target.GetComponent<BoxCollider2D>();

        // Coroutine for A*
        StartCoroutine(A_star());
    }

    // Update is called once per frame
    void Update()
    {
        if(self_box.IsTouching(target_box) == false 
                && (bool)Variables.Object(gameObject).Get("TrackingPlayer") == true){

            if((float)Variables.Object(gameObject).Get("reachedPositionDistance") != 0){
                Variables.Object(gameObject).Set("reachedPositionDistance", 0f);
                Variables.Object(gameObject).Set("AstartPosition", target.transform.position);
            }

            // if has path
            if(next_node != null){
                // Call the AddForce function of our Rigidbody2D rb2d 
                // supplying movement multiplied by speed to move. 
                //rb2d.AddForce(next_node.direction * speedMultiplier);

                // change to next node only after pass a node
                if(pass_node(next_node) == true){
                    if(path.ToArray().Length != 0){
                        // take last node store in path and remove it
                        next_node = path[path.ToArray().Length - 1];
                        path.RemoveAt(path.ToArray().Length - 1);

                        // set visual script
                        Variables.Object(gameObject).Set("AstartPosition", 
                            new Vector3(next_node.position[0], next_node.position[1], 0));
                    }
                    else{
                        next_node = null;
                    }
                }
            }
            // go to the palyer position until A* done
            else{
                Variables.Object(gameObject).Set("AstartPosition", target.transform.position);
            }
        }
        // empty path after touch the target
        else{
            path = new List<Node>();
            next_node = null;
        }
    }

    IEnumerator A_star(){
        while(true){
        if(self_box.IsTouching(target_box) == false
                && (bool)Variables.Object(gameObject).Get("TrackingPlayer") == true){
        // location of self and target
        Vector2 start = transform.position;
        Vector2 end = target.transform.position;

        // nodes to be searched
        List<Node> open = new List<Node>();
        // nodes already be searched
        List<Node> close = new List<Node>();

        // start A*
        open.Add(new Node(start, end));
        Node current = open[0];

        int minimum_index;
        float minmum_cost;

        // distance weight, depends distance to target
        float weight = 1f;

        int A_count = 0;

        while(target_box.OverlapPoint(current.position) == false){
            // find minimum cost in open list be the current node, remove it and add to close
            minimum_index = 0;
            minmum_cost = open[0].total_cost;
            if(open.ToArray().Length > 1){
                for(int i=1; i < open.ToArray().Length; i++){
                    if(open[i].total_cost < minmum_cost){
                        minimum_index = i;
                        minmum_cost = open[i].total_cost;
                    }
                }
            }

            current = open[minimum_index];
            open.RemoveAt(minimum_index);
            close.Add(current);

            // if reach the target
            if(target_box.OverlapPoint(current.position) == true){
                break;
            }

            // 9 possible move
            for(int x=-1; x<=1; x++){
                for(int y=-1; y<=1; y++){
                    // skip stop move
                    if(x == 0 && y ==0){
                        continue;
                    }

                    Vector2 direction = new Vector2(x, y);
                    float minimum_unit = 0.5f;
                    Vector2 new_location = current.position + new Vector2(x*minimum_unit, y*minimum_unit);

                    // if in close list, skip
                    if (close.Exists(t => t.position == new_location) == true){
                        continue;
                    }

                    // if hit wall, skip
                    if(touch_detect(new_location, "Environment") == true){
                        continue;
                    }

                    // calculate cost
                    float cost;
                    if((System.Math.Abs(x) + System.Math.Abs(y)) == 1){
                        cost = minimum_unit;
                    }
                    else{
                        cost = minimum_unit * 1.4f;
                    }

                    string[] terrains = {"Terrain_speed_down"};
                    for(int i=0; i < terrains.Length; i++){
                        GameObject terrain_speed_down = GameObject.FindWithTag(terrains[i]);
                        if(terrain_speed_down != null){
                            float terrain_costs = terrain_speed_down.GetComponent<move_cost>().cost;

                            if(touch_detect(new_location, terrains[i]) == true){
                                cost = cost * terrain_costs;
                            }
                        }
                    }

                    Node new_node = new Node(new_location, end, cost, current, direction, weight);

                    // if not in open list, add in
                    if (open.Exists(t => t.position == new_location) == false)
                    {
                        open.Add(new_node);
                    }
                    // if in open list, compare cost
                    else{
                        int index = open.FindIndex(t => t.position == new_location);
                        // if new node has lower cost, replace the original
                        if(open[index].total_cost > new_node.total_cost){
                            open.RemoveAt(index);
                            open.Add(new_node);
                        }
                    }
                }
            }

            A_count++;
            if(A_count > 5 && (start - end).magnitude > 30f){
                A_count = 0;
                yield return null;
            }
            else if(A_count > 100 && (start - end).magnitude > 15f){
                A_count = 0;
                yield return null;
            }
        }

        List<Node> new_path = new List<Node>();

        // the final Node will contain list of A* path
        while(current.parent != null){
            new_path.Add(current);
            current = current.parent;
        }

        path = new_path;

        // take last node store in path and remove it
        next_node = path[path.ToArray().Length - 1];
        path.RemoveAt(path.ToArray().Length - 1);

        // set visual script
        Variables.Object(gameObject).Set("AstartPosition", 
            new Vector3(next_node.position[0], next_node.position[1], 0));
        }
        
        Vector2 current_position = transform.position;
        Vector2 target_position = target.transform.position;
        // timing run A* to relocate target, depend on distance to target
        if((current_position - target_position).magnitude < 15f){
            yield return new WaitForSeconds(1);
        }
        else if((current_position - target_position).magnitude < 30f){
            yield return new WaitForSeconds(5);
        }
        else if((current_position - target_position).magnitude > 30f){
            yield return new WaitForSeconds(10);
        }
        
        }
    }

    bool pass_node(Node node){
        float x = transform.position[0];
        float y = transform.position[1];

        if(node.direction[0] == -1){
            if(x > node.position[0]){
                return false;
            }
        }
        else if(node.direction[0] == 1){
            if(x < node.position[0]){
                return false;
            }
        }

        if(node.direction[1] == -1){
            if(y > node.position[1]){
                return false;
            }
        }
        else if(node.direction[1] == 1){
            if(y < node.position[1]){
                return false;
            }
        }

        return true;
    }

    bool touch_detect(Vector2 center, string terrain){
        // 2 corners
        Vector2 left_down = new Vector2(center[0] + offset_x - (box_x * 0.5f),
                                    center[1] + offset_y - (box_y * 0.5f));

        Vector2 right_up = new Vector2(center[0] + offset_x + (box_x * 0.5f),
                                    center[1] + offset_y + (box_y * 0.5f));

        // use 4 ray to simulate collider box
        RaycastHit2D left = Physics2D.Raycast(left_down, new Vector2(0, 1), box_y, LayerMask.GetMask(terrain));
        RaycastHit2D down = Physics2D.Raycast(left_down, new Vector2(1, 0), box_x, LayerMask.GetMask(terrain));
        RaycastHit2D right = Physics2D.Raycast(right_up, new Vector2(0, -1), box_y, LayerMask.GetMask(terrain));
        RaycastHit2D up = Physics2D.Raycast(right_up, new Vector2(-1, 0), box_x, LayerMask.GetMask(terrain));

        if(left.collider != null || down.collider != null || right.collider != null|| up.collider != null){
            return true;
        }
        
        return false;
    }
}
