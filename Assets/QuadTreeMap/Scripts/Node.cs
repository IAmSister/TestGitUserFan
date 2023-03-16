using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WCC.QuadTree
{
    public class Node : INode
    {
        public Bounds bound { get; set; }

        private int depth; //当前深度
        private Tree belongTree; //父节点
        private Node[] childList; //关系域
        private List<ObjData> objDataList; //数据域
        private bool isInside = false;

        public Node(Bounds bound, int depth, Tree belongTree)
        {
            this.belongTree = belongTree;
            this.bound = bound;
            this.depth = depth;
            objDataList = new List<ObjData>();
        }
        //第一个国家 第二个省份包含城市
        public void InsertObjData(ObjData objData)
        {
            //判断节点是否到了最小管理单元，并且 这个节点没有孩子
            // 节点管理区域   节点属于某个孩子区域  
            //                节点属于多个孩子区域
            Node node = null;
            bool bChild = false;
            // 当前深度小于 父节点深度    并 且没有子节点 （到叶子节点）
            if (depth < belongTree.maxDepth && childList == null)
            {
                CreateChild();
            }
            //遍历四个孩子
            if (childList != null)
            {
                for (int i = 0; i < childList.Length; ++i)
                {
                    Node item = childList[i];
                    // if (item.bound.Contains(objData.pos))
                    //表示前面已经有人管理了，有属于另外一个孩子的管理管理区域 包含
                    if (item.bound.Intersects(objData.GetObjBounds()))
                    {
                        //表示前面已经有人管理了 有属于另一个孩子管理区域
                        if (node != null)
                        {
                            bChild = false; //记录到这一层
                            break;
                        }
                        node = item; //第一次找到了一个孩子，可以管理物体
                        bChild = true;
                    }
                }
            }
            // 我们物体，完全属于一个孩子
            if (bChild)  //递归
            {
                node.InsertObjData(objData);
            }
            else //物体属于多个孩子，所以我们加到自己这里来  ，最终到叶子节点，也会走这里。
            {
                objDataList.Add(objData);
            }
        }

        //在该节点里
        public void Inside(Camera camera)
        {
            //刷新子节点
            if (childList != null)
            {
                for (int i = 0; i < childList.Length; ++i)
                {
                    if (childList[i].bound.CheckBoundIsInCamera(camera, belongTree.viewRatio))
                    {
                        childList[i].Inside(camera);
                    }
                    else
                    {
                        childList[i].Outside(camera);
                    }
                }
            }

            if (isInside)
                return;
            isInside = true;
            for (int i = 0; i < objDataList.Count; ++i)
            {
                ObjManager.Instance.LoadAsync(objDataList[i]);
            }

        }

        //不在该节点里
        public void Outside(Camera camera)
        {
            //刷新子节点
            if (childList != null)
            {
                for (int i = 0; i < childList.Length; ++i)
                {
                    childList[i].Outside(camera);
                }
            }
            if (isInside == false)
                return;
            isInside = false;
            for (int i = 0; i < objDataList.Count; i++)
            {
                ObjManager.Instance.Unload(objDataList[i].uid);
            }
        }
        /// <summary>
        /// 创建节点
        /// </summary>
        private void CreateChild()
        {
            
            childList = new Node[belongTree.maxChildCount];
            int index = 0;
            
            for (int i = -1; i <= 1; i += 2)
            {
                for (int j = -1; j <= 1; j += 2)
                {
                    Vector3 centerOffset = new Vector3(bound.size.x / 4 * i, 0, bound.size.z / 4 * j);
                    Vector3 cSize = new Vector3(bound.size.x / 2, bound.size.y, bound.size.z / 2);
                    Bounds cBound = new Bounds(bound.center + centerOffset, cSize);
                    childList[index++] = new Node(cBound, depth + 1, belongTree);
                }
            }
        }

        public void DrawBound()
        {
            if (isInside)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(bound.center, bound.size);
            }
            else if (objDataList.Count != 0)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireCube(bound.center, bound.size);
            }
            else
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(bound.center, bound.size);
            }

            if (childList != null)
            {
                for (int i = 0; i < childList.Length; ++i)
                {
                    childList[i].DrawBound();
                }
            }
        }
    }
}