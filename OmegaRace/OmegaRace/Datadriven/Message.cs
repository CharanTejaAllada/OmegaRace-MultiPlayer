using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using CollisionManager;
 
namespace OmegaRace
{


    abstract class Message
    {
        abstract public QueueType getQueuetype();
        abstract public void execute();
        
    }

    public struct physics_buffer 
    {
        public Vector2 position;
        public float rotation;
        public int id;

    }

    abstract class Ship_Message : Message
    {
        public CollisionManager.PlayerID id;

        public Ship_Message(CollisionManager.Player pPlayer)
        {
            this.id = pPlayer.id;
        }

        public Ship_Message(CollisionManager.PlayerID p)
        {
            this.id = p;
        }
    }

    //------------------------------------------------------------------------------------

    class physics_buffer_message : Message
    {
        public int count;
        public physics_buffer[] buff;
        public static physics_buffer_message pBuffGlobal = null;

        public physics_buffer_message(ref physics_buffer[] p)
        {
            this.count = p.Length;
            this.buff = p; 
        }

        public physics_buffer_message(physics_buffer_message p)
        {
            this.count = p.count;
            this.buff = p.buff;
        }

        public override QueueType getQueuetype()
        {
            return QueueType.playerBuffer;   
        }

        public override void execute()
        {
            pBuffGlobal = this;
        }
    }



    class ship_impulse : Ship_Message 
    {
        public Vector2 impulse;
        public float x;
        public float y;
        public float rot;

        public ship_impulse(CollisionManager.Player p,Vector2 imp) :base(p.id)
        {
            this.impulse.X=imp.X;
            this.impulse.Y=imp.Y;
        }

        public override QueueType getQueuetype()
        {
            return QueueType.ship_impulse;
        }

        public override void execute()
        {
            CollisionManager.Player p = PlayerManager.getPlayer(this.id);
            p.playerShip.physicsObj.body.ApplyLinearImpulse(this.impulse,p.playerShip.physicsObj.body.GetWorldCenter());
        }
    }

    class EvenMessage : Message
    {
        public int gameIdA;
        public int gameIdB;
        public Vector2 CollisionPt;

        public EvenMessage(EvenMessage msg)
        {
            this.gameIdA = msg.gameIdA;
            this.gameIdB = msg.gameIdB;
            this.CollisionPt = msg.CollisionPt;
        }


        public EvenMessage(int gameA, int gameB, Vector2 pt)
        {
            this.gameIdA = gameA;
            this.gameIdB = gameB;
            this.CollisionPt = pt;
        }

        public override QueueType getQueuetype()
        {
            return QueueType.EventMessage;
        }

        public override void execute()
        {
            GameObject A = GameObjManager.FindByID(this.gameIdA);
            GameObject B = GameObjManager.FindByID(this.gameIdB);

            if (A != null && B != null)
            {
                if (A.CollideAvailable == true && B.CollideAvailable == true)
                {
                    if (A.type < B.type)
                    {
                        A.Accept(B, CollisionPt);
                    }
                    else
                    {
                        B.Accept(A, CollisionPt);
                    }
                }
            }
        }
    }

    class Ship_Create_Missile_Message : Ship_Message
    {
        public float x;
        public float y;
        public float rot;

        public Ship_Create_Missile_Message(CollisionManager.Player p)
            : base(p)
        {
        
        }

        public override QueueType getQueuetype()
        {
            return QueueType.ship_missile;
        }

        public override void execute()
        {
            
        }
    }

    class Ship_Create_Bomb_Message : Ship_Message
    {
        public float x;
        public float y;
        public float rot;

        public Ship_Create_Bomb_Message(CollisionManager.Player p):base(p)
        {
            
        }

        public override QueueType getQueuetype()
        {
            return QueueType.ship_bomb;
        }

        public override void execute()
        {
            
        }
    }

    class Ship_rot_message : Ship_Message
    {
        public float rot;
        public float serverRotvalue;
        public float x;
        public float y;



        public Ship_rot_message(Ship_rot_message pObj) :base(pObj.id)
        {
            this.rot = pObj.rot;
        }

        public Ship_rot_message(CollisionManager.Player p, float rotation)
            : base(p.id)
        {
            this.rot = rotation;
        }

        public override QueueType getQueuetype()
        {
            return QueueType.ship_rot;
        }

        public override void execute()
        {
            CollisionManager.Player p = PlayerManager.getPlayer(this.id);
            p.playerShip.physicsObj.body.Rotation += this.rot;
        }
    }

}
