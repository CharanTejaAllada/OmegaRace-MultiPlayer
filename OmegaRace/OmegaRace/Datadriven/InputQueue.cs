using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Net;
using CollisionManager;
using System.Diagnostics;





namespace OmegaRace
{

    public struct qHeader
    {
        public Object obj;
        public QueueType type;
        public int outseq;
        public int inseq;
        public PlayerID packetOwner;

    }



    public enum QueueType
    {
        bomb,
        missile,
        ship_rot,
        ship_rot_clock,
        ship_rot_anti,
        ship_impulse,
        ship_missile,
        playerBuffer,
        EventMessage,
        physicsBuffer,
        ship_bomb
    }

    //-----------------------------------------------------------
    class inQueue
    {

        public static void add(object pObject, QueueType pQueue, int outSeqNum, PlayerID pID)
        {
            qHeader pHeader;
            pHeader.packetOwner = pID;
            pHeader.type = pQueue;
            pHeader.inseq = InputQueue.seqNumGlobal;
            pHeader.outseq = outSeqNum;
            pHeader.obj = pObject;
            InputQueue.seqNumGlobal++;
            InputQueue.inpQ.Enqueue(pHeader);
        }
    }
    //-------------------------------------------------------------------

    //-------------------------------------------

    class InputQueue
    {

        static public CollisionManager.Player[] player = new CollisionManager.Player[2];
        static public Queue<qHeader> inpQ = new Queue<qHeader>();
        static public int seqNumGlobal = 3111;



           public void process()
           {
               int count = inpQ.Count;
               
               CollisionManager.GameObjManager pObj = CollisionManager.GameObjManager.Instance();
               
               for (int i = 0; i < count; i++)
               {
                   qHeader pInstance = inpQ.Dequeue();

                   switch (pInstance.type)
                   {
                      
                       case QueueType.ship_impulse:
                           ship_impulse imp = (ship_impulse)pInstance.obj;
                                             imp.execute();
                                             break;
                       case QueueType.ship_missile:
                                             Ship_Create_Missile_Message smiss = (Ship_Create_Missile_Message)pInstance.obj;
                                             CollisionManager.Player p2 = PlayerManager.getPlayer(smiss.id);
                                             p2.createMissile();
                                             break;
                       case QueueType.ship_rot_anti:
                                             Ship_rot_message srmsg = (Ship_rot_message)pInstance.obj;
                                             srmsg.rot = -0.1f;
                                             srmsg.execute();
                                             break;
                       case QueueType.ship_bomb:
                                             Ship_Create_Bomb_Message smsg = (Ship_Create_Bomb_Message)pInstance.obj;
                                             CollisionManager.GameObjManager.Instance().createBomb(smsg.id);
                                             break;
                      
                
                       case QueueType.EventMessage:
                                             EvenMessage msg = (EvenMessage)pInstance.obj;
                                             msg.execute();
                                             break;
                       case QueueType.ship_rot_clock:
                                             Ship_rot_message p3 = (Ship_rot_message)pInstance.obj;
                                             p3.rot = 0.1f;
                                             p3.execute();
                                             break;

                   }
               }
           }
          
             
    }



    }

