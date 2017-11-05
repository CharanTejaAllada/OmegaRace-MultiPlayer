using System;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpriteAnimation;
using Box2D.XNA;
using OmegaRace;

namespace CollisionManager
{
    enum GameObjType
    {   
        //gameObj,
        p1missiles,   // can only collide once
        p2missiles,   // can only collide once
        p1ship,
        p2ship,
        horzWalls,
        vertWalls,
        bomb,
        fencePost,
        DESTROY,
        explosion,
        p1Bomb,
        p2Bomb,
        None
    }

    class GameObjManager : Manager
    {
        public int count = 1;
        private static GameObjManager instance;

        private BodyNode destroyHead;


        private GameObjManager()
        {
            destroyHead = null;
        }

        public static GameObjManager Instance()
        {
            if (instance == null)
                instance = new GameObjManager();
            return instance;
        }

        // For Linked List
        public void addGameObj(GameObject _obj)
        {

            GameObjNode node = new GameObjNode();
            node.Set(_obj);
            node.gameObj.gameId = count++;
            this.privActiveAddToFront((ManLink)node, ref this.active);
        }

        public void Update(World w)
        {
            ManLink ptr = this.active;

            GameObjNode gameObjNode = null;

            while (ptr != null)
            {
                gameObjNode = (GameObjNode)ptr;

                gameObjNode.gameObj.Update();

                ptr = ptr.next;
            }


            destroyBodies(w);
        }


        public void remove(batchEnum _enum, GameObject _obj)
        {
            GameObjNode node = findGameObj(_obj);

            if (node != null)
            {
                if (node.prev != null)
                {	// middle or last node
                    node.prev.next = node.next;
                }
                else
                {  // first
                    this.active = node.next;
                }

                if (node.next != null)
                {	// middle node
                    node.next.prev = node.prev;
                }

                addBodyToDestroy(_obj.physicsObj.body);

                if (_obj.spriteRef != null)
                {
                    SBNode SBNode = SpriteBatchManager.Instance().getBatch(_enum);
                    SBNode.removeDisplayObject(_obj.spriteRef);
                }

                if (_obj.physicsObj != null)
                {
                    PhysicsMan.Instance().removePhysicsObj(_obj.physicsObj);
                }
            }
        }

        public void UpdatePlayerObject(GameObject _obj)
        {
            ManLink ptr = this.active;

            //ManLink outNode = null;
            
            while (ptr != null)
            {
                if ((ptr as GameObjNode).gameObj.Equals(_obj))
                {
                    //ptr  = _obj; 
                    //Console.WriteLine(" Player type :"+_obj.type+" \nRotation "+_obj.rotation+" Location "+_obj.location+" Speed "+_obj.objSpeed);
                    break;
                }
                ptr = ptr.next;
            }
        }

        private GameObjNode findGameObj(GameObject _obj)
        {
            ManLink ptr = this.active;

            ManLink outNode = null;

            while (ptr != null)
            {
                if ((ptr as GameObjNode).gameObj.Equals(_obj))
                {
                    outNode = ptr;
                    break;
                }
                ptr = ptr.next;
            }

            return (outNode as GameObjNode);

        }

        public void addBodyToDestroy(Body b)
        {
            BodyNode bodyNode = new BodyNode(b);

            if (destroyHead == null)
            {
                destroyHead = bodyNode;
                bodyNode.next = null;
                bodyNode.prev = null;
            }
            else
            {
                bodyNode.next = destroyHead;
                destroyHead.prev = bodyNode;
                destroyHead = bodyNode;
            }
        }


        public void destroyBodies(World w)
        {
            BodyNode ptr = destroyHead;

            while (ptr != null)
            {
                w.DestroyBody(ptr.body);

                ptr = ptr.next;
            }

            destroyHead = null;
        }

        public void createBomb(PlayerID _id)
        {
            Player player = PlayerManager.getPlayer(_id);
            OutputQueue outqueue = new OutputQueue();

            player.removeBombSprite();

            Ship pShip = player.playerShip;
            Body pShipBody = pShip.physicsObj.body;

             Bomb bomb;

            if (_id == PlayerID.one)
                bomb = new Bomb(GameObjType.p1Bomb, _id, pShip);
            else
                bomb = new Bomb(GameObjType.p2Bomb, _id, pShip);

            OutQueue.add(QueueType.bomb ,bomb,_id);
        }

        


        public void addExplosion(Vector2 _pos, Color _color)
        {

            Sprite expSprite = (Sprite)DisplayManager.Instance().getDisplayObj(SpriteEnum.Explosion);
            Sprite_Proxy expProxy = new Sprite_Proxy(expSprite, (int)_pos.X, (int)_pos.Y, 0.20f, _color);

            SBNode expBatch = SpriteBatchManager.Instance().getBatch(batchEnum.explosions);
            expBatch.addDisplayObject(expProxy);

            TimeSpan currentTime = Timer.GetCurrentTime();
            TimeSpan t_1 = currentTime.Add(new TimeSpan(0, 0, 0, 0, 500));
            CallBackData nodeData = new CallBackData(3, TimeSpan.Zero);
            nodeData.spriteRef = expProxy;

            Timer.Add(t_1, nodeData, removeExplosion);


        }

        public void removeExplosion(object obj)
        {
            CallBackData nodeData = (CallBackData)obj;

            SBNode sbNode = SpriteBatchManager.Instance().getBatch(batchEnum.explosions);
            sbNode.removeDisplayObject(nodeData.spriteRef);
        }

        protected override object privGetNewObj()
        {
            throw new NotImplementedException();
        }
        
        public void clear()
        {
            this.active = null;
            instance = null;
        }


        public static GameObject FindByID(int p)
        {
            ManLink ptr = instance.active;

            ManLink outNode = null;
            GameObject g=null;

            while (ptr != null)
            {
                if ((ptr as GameObjNode).gameObj.gameId == p)
                {
                    outNode = ptr;
                    g = (outNode as GameObjNode).gameObj;
                    break;
                }
                ptr = ptr.next;
            }
            return g;
        }
    }
}
