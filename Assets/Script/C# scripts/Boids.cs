using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boids : MonoBehaviour
{
    // Floating point variable to store the player's movement speed. 
    public float speedMultiplier;

    // object and location of target
    public GameObject target;

    // Store a reference to the Rigidbody2D component required to use 2D Physics. 
    private Rigidbody2D rb2d;
    // referece to the BoxCollider2D
    private BoxCollider2D self_box;
    private float box_x;
    private float box_y;
    private float offset_x;
    private float offset_y;

    // list of boids and index of self
    private Boids_list boids_C;
    private List<GameObject> boids_L;
    private int L_index;

    // direction to move
    private Vector2 direction = new Vector2(0, 0);

    // Start is called before the first frame update
    void Start()
    {
        // Get and store a reference to the Rigidbody2D component 
        // so that we can access it. 
        rb2d = GetComponent<Rigidbody2D>(); 
        self_box = GetComponent<BoxCollider2D>();
        box_x = self_box.size[0] * transform.lossyScale.x;
        box_y = self_box.size[1] * transform.lossyScale.y;
        offset_x = self_box.offset[0];
        offset_y = self_box.offset[1];

        // get boids list
        boids_C = target.GetComponent<Boids_list>(); 

        // update to list
        boids_C.add(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        boids();
        obstacle_avoidance();
        // if target too far away, speed up
        if((target.transform.position - transform.position).magnitude > 5f){
            direction = direction * 3f;
        }
        rb2d.AddForce(direction * speedMultiplier);
    }

    public int get_index(){
        return L_index;
    }

    public void set_index(int index){
        L_index = index;
    }

    public void get_boids_list(){
        boids_L = boids_C.get_list();
    }

    void boids(){
        int separation_count = 0;
        Vector2 separation_direction = new Vector2(0, 0);

        int alignment_count = 0;
        Vector2 alignment_direction = new Vector2(0, 0);

        int cohesion_count = 0;
        Vector2 cohesion_direction = new Vector2(0, 0);

        Vector2 self_location = transform.position;

        for(int i=0; i < boids_L.ToArray().Length; i++){
            // skip self
            if(boids_L[i] == gameObject){
                continue;
            }

            Vector2 coordinate_difference = (boids_L[i].transform.position - transform.position);
            float distance = coordinate_difference.magnitude;
            
            // separation
            if(distance < boids_C.separation_distance){
                separation_direction += coordinate_difference;
                separation_count++;
            }

            // alignment and cohesion
            if(distance < boids_C.alignment_distance){
                Vector2 boids_forward = boids_L[i].GetComponent<Boids>().get_direction();
                alignment_direction += boids_forward;
                alignment_count++;

                cohesion_direction += coordinate_difference;
                cohesion_count++;
            }
        }

        // calculate average
        if(separation_count > 1){
            separation_direction = separation_direction / separation_count;
        }

        if(alignment_count > 1){
            alignment_direction = alignment_direction / alignment_count;
        }

        if(cohesion_count > 1){
            cohesion_direction = cohesion_direction / cohesion_count;
        }

        cohesion_direction -= self_location;

        direction = -(separation_direction.normalized * 0.2f);
        direction += (alignment_direction.normalized * 0.15f);
        direction += (cohesion_direction.normalized * 0.15f);
        
        // follow the target
        Vector2 follow_direction = (target.transform.position - transform.position);

        // if too close, avoid the target
        if(follow_direction.magnitude < boids_C.separation_distance){
            direction += -(follow_direction.normalized * 0.5f);
        }
        // otherwise, follow the target
        else{
            direction += (follow_direction.normalized * 0.5f);
        }
    }

    Vector2 get_direction(){
        return direction;
    }

    void obstacle_avoidance(){
        Vector2 self_location = transform.position;

        Vector2 new_location = self_location + direction;
        
        // avoid obstacle by moving 8 directions and try to move toward target
        if(touch_detect(new_location, "Environment") == true 
                || self_box.IsTouchingLayers(LayerMask.GetMask("Environment")) == true){
            Vector2 follow_direction = (target.transform.position - transform.position);
            float x = 0;
            float y = 0;

            // if a direction no obstacle, move to this direction
            // x
            if(follow_direction[0] > 0){
                x = 1;
            }
            else if(follow_direction[0] < 0){
                x = -1;
            }
            
            // y
            if(follow_direction[1] > 0){
                y = 1;
            }
            else if(follow_direction[1] < 0){
                y = -1;
            }

            // if oblique direction has obstacle, change to straight direction
            new_location = self_location + new Vector2(x, y);
            if(touch_detect(new_location, "Environment") == true){

                new_location = self_location + new Vector2(x, 0);
                if(touch_detect(new_location, "Environment") == true){
                    x = 0;
                }
                
                new_location = self_location + new Vector2(0, y);
                if(touch_detect(new_location, "Environment") == true){
                    y = 0;
                }
            }

            // if no legal direction, try "flee" force
            if(x == 0 & y == 0){
                direction = -direction * 2f;
            }
            else{
                // speed up
                direction = new Vector2(x * 2f, y * 2f);
            }
        }
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
