using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NEAT
{
    public class NEAT
    {
        public Activation.ActivationType ActivationFunction;

        private List<Node> _nodes;
        private List<Connection> _connections;
        private int _inputCount;
        private int _outputCount;
        private int _nextNodeID;
        private int _nextConnectionID;
        private double _compatibilityThreshold;
        private double _c1; // Excess Coefficient
        private double _c2; // Disjoint Coefficient
        private double _c3; // Weight Coefficient

        public double Fitness = 0;

        public static Random Random = new System.Random();

        public NEAT(int inputCount, int outputCount)
        {
            _nodes = new List<Node>();
            _connections = new List<Connection>();
            _inputCount = inputCount;
            _outputCount = outputCount;
            _nextNodeID = inputCount + outputCount + 1;
            _nextConnectionID = 1;
            _compatibilityThreshold = 3.0;
            _c1 = 1.0;
            _c2 = 1.0;
            _c3 = 0.4;
            for (int i = 1; i <= inputCount; i++)
            {
                _nodes.Add(new Node(i, NodeType.Input, ActivationFunction));
            }
            for (int i = 1; i <= outputCount; i++)
            {
                _nodes.Add(new Node(_inputCount + i, NodeType.Output,ActivationFunction));
            }
        }

        public NEAT(List<Node> childNodes, List<Connection> childGenes, int inputCount, int outputCount)
        {
            _nodes = new List<Node>();
            _connections = new List<Connection>();
            _inputCount = inputCount;
            _outputCount = outputCount;
            _nextNodeID = inputCount + outputCount + 1;
            _nextConnectionID = 1;
            _compatibilityThreshold = 3.0;
            _c1 = 1.0;
            _c2 = 1.0;
            _c3 = 0.4;
            for (int i = 1; i <= inputCount; i++)
            {
                _nodes.Add(new Node(i, NodeType.Input, ActivationFunction));
            }
            for (int i = 1; i <= outputCount; i++)
            {
                _nodes.Add(new Node(_inputCount + i, NodeType.Output,ActivationFunction));
            }
        }

        public void Mutate()
        {
            if (Random.NextDouble() < 0.05)
            {
                AddNode();
            }
            if (Random.NextDouble() < 0.1)
            {
                AddConnection();
            }
            foreach (Node node in _nodes)
            {
                if (Random.NextDouble() < 0.1)
                {
                    node.Mutate();
                }
            }
            foreach (Connection connection in _connections)
            {
                if (Random.NextDouble() < 0.1)
                {
                    connection.Mutate();
                }
            }
        }

        private void AddNode()
        {
            if (_connections.Count == 0)
            {
                return;
            }
            Connection connection = _connections[Random.Next(_connections.Count)];
            Node inputNode = _nodes.Find(node => node.ID == connection.InputNodeID);
            Node outputNode = _nodes.Find(node => node.ID == connection.OutputNodeID);
            connection.Disable();
            Node newNode = new Node(_nextNodeID, NodeType.Hidden,ActivationFunction);
            _nodes.Add(newNode);
            _nextNodeID++;
            _connections.Add(new Connection(connection.InputNodeID, newNode.ID, 1.0, _nextConnectionID));
            _nextConnectionID++;
            _connections.Add(new Connection(newNode.ID, connection.OutputNodeID, connection.Weight, _nextConnectionID));
            _nextConnectionID++;
        }

        private void AddConnection()
        {
            List<Node> inputNodes = _nodes.Where(node => node.Type == NodeType.Input).ToList();
            List<Node> outputNodes = _nodes.Where(node => node.Type == NodeType.Output).ToList();
            Node inputNode = inputNodes[Random.Next(inputNodes.Count)];
            Node outputNode = outputNodes[Random.Next(outputNodes.Count)];
            if (IsDuplicateConnection(inputNode.ID, outputNode.ID))
            {
                return;
            }
            _connections.Add(new Connection(inputNode.ID, outputNode.ID, Random.NextDouble() * 2 - 1, _nextConnectionID));
            _nextConnectionID++;
        }

        private bool IsDuplicateConnection(int inputNodeID, int outputNodeID)
        {
            foreach (Connection connection in _connections)
            {
                if (connection.InputNodeID == inputNodeID && connection.OutputNodeID == outputNodeID)
                {
                    return true;
                }
            }
            return false;
        }

        private Node GetNode(int id)
        {
            foreach (Node node in _nodes)
            {
                if (node.ID == id)
                {
                    return node;
                }
            }
            return null;
        }

        private Connection GetConnection(int id)
        {
            foreach (Connection connection in _connections)
            {
                if (connection.ID == id)
                {
                    return connection;
                }
            }
            return null;
        }
        
        // returns the outputs of the entire network
        public double[] FeedForward(double[] inputs) 
        {
            // Set the input node values
            for (int i = 0; i < inputs.Length; i++)
            {
                Node inputNode = GetNode(i + 1);
                inputNode.ActivationLevel = inputs[i];
            }

            // Activate the hidden and output nodes
            foreach (Node node in _nodes)
            {
                if (node.Type == NodeType.Hidden || node.Type == NodeType.Output)
                {
                    double activation = 0.0;
                    foreach (Connection connection in _connections)
                    {
                        if (connection.OutputNodeID == node.ID && connection.IsEnabled)
                        {
                            Node inputNode = GetNode(connection.InputNodeID);
                            activation += connection.Weight * inputNode.ActivationLevel;
                        }
                    }
                    node.ActivationLevel = node.ActivationFunction.Activate(activation);
                }
            }

            // Return the output node values
            double[] outputs = new double[_outputCount];
            for (int i = 0; i < _outputCount; i++)
            {
                Node outputNode = GetNode(_inputCount + i + 1);
                outputs[i] = outputNode.ActivationLevel;
            }
            return outputs;
        }

        private double CalculateCompatibilityDistance(NEAT other)
        {
            int disjointGenes = 0;
            int excessGenes = 0;
            double weightDifference = 0.0;
            int matchingGenes = 0;

            int thisGeneIndex = 0;
            int otherGeneIndex = 0;
            
            while (thisGeneIndex < _connections.Count || otherGeneIndex < other._connections.Count) {
                if (thisGeneIndex == _connections.Count) {
                    excessGenes++;
                    otherGeneIndex++;
                } else if (otherGeneIndex == other._connections.Count) {
                    excessGenes++;
                    thisGeneIndex++;
                } else {
                    Connection thisGene = _connections[thisGeneIndex];
                    Connection otherGene = other._connections[otherGeneIndex];
                    if (thisGene.ID == otherGene.ID) {
                        matchingGenes++;
                        weightDifference += Math.Abs(thisGene.Weight - otherGene.Weight);
                        thisGeneIndex++;
                        otherGeneIndex++;
                    } else if (thisGene.ID < otherGene.ID) {
                        disjointGenes++;
                        thisGeneIndex++;
                    } else {
                        disjointGenes++;
                        otherGeneIndex++;
                    }
                }
            }

            int maxGenes = Math.Max(_connections.Count, other._connections.Count);
            double excessFactor = _c1 * excessGenes / maxGenes;
            double disjointFactor = _c2 * disjointGenes / maxGenes;
            double weightFactor = _c3 * weightDifference / matchingGenes;

            return excessFactor + disjointFactor + weightFactor;
        }

        public void setFitness(double newFitness)
        {
            Fitness = newFitness;
        }

        public NEAT Breed(NEAT other)
        {
            // Determine the fitter parent NEAT
            NEAT parent1 = this.Fitness >= other.Fitness ? this : other;
            NEAT parent2 = this.Fitness < other.Fitness ? this : other;

            // Initialize the genes and nodes for the offspring
            List<Connection> childGenes = new List<Connection>();
            Dictionary<int, Node> childNodes = new Dictionary<int, Node>();

            // Iterate over the genes in the fitter parent NEAT
            for (int i = 0; i < parent1._connections.Count; i++)
            {
                Connection gene1 = parent1._connections[i];
                Connection gene2 = parent2.GetConnection(gene1.ID);

                // Use the gene from the fitter parent if it is enabled
                // Otherwise, randomly select one of the genes to use
                Connection childGene = null;
                if (gene2 != null && gene2.IsEnabled)
                {
                    childGene = gene1.IsEnabled ? Random.NextDouble() < 0.5 ? gene1 : gene2 : gene2;
                }
                else
                {
                    childGene = gene1;
                }

                // Add the child gene to the list of offspring genes
                childGenes.Add(childGene);

                // Add the child gene's input and output nodes to the list of offspring nodes
                if (!childNodes.ContainsKey(childGene.InputNodeID))
                {
                    Node inputNode = GetNode(childGene.InputNodeID).Clone();
                    childNodes.Add(inputNode.ID, inputNode);
                }
                if (!childNodes.ContainsKey(childGene.OutputNodeID))
                {
                    Node outputNode = GetNode(childGene.OutputNodeID).Clone();
                    childNodes.Add(outputNode.ID, outputNode);
                }
            }

            // Create a new offspring NEAT using the child genes and nodes
            NEAT child = new NEAT(childNodes.Values.ToList(), childGenes, _inputCount, _outputCount);

            // Mutate the offspring NEAT
            child.Mutate();

            return child;
        }


    }

    public class Node
    {
        public int ID;
        public NodeType Type;
        public double Value;
        public double ActivationLevel;
        public Activation.ActivationType ActivationType;
        public IActivation ActivationFunction;

        public Node(int id, NodeType type, Activation.ActivationType activationType)
        {
            ID = id;
            Type = type;
            Value = 0.0;
            ActivationType = activationType;
            ActivationFunction = Activation.GetActivationFromType(activationType);
        }

        public void Mutate()
        {
            switch (Type)
            {
                case NodeType.Hidden:
                    // Mutate activation function with probability 0.1
                    if (NEAT.Random.NextDouble() < 0.1)
                    {
                        // Replace activation function with a new random one
                        
                    }
                    break;
                case NodeType.Input:
                case NodeType.Output:
                default:
                    // Input and output nodes have fixed activation functions
                    break;
            }
        }

        public Node Clone()
        {
            Node clone = new Node(ID, Type, ActivationType);
            clone.Value = Value;
            clone.ActivationFunction = ActivationFunction;
            clone.ActivationLevel = ActivationLevel;
            return clone;
        }


    }

    public enum NodeType
    {
        Input,
        Hidden,
        Output
    }

    public class Connection
    {
        public int ID;
        public int InputNodeID;
        public int OutputNodeID;
        public double Weight;
        public bool IsEnabled;
        public bool IsRecurrent;
        public double PerturbChance;
        public double MaxWeightPerturbation;
        public double CompatibilityThreshold;

        public Connection(int inputNode, int outputNode, double weight,int ConnectionID)
        {
            ID = ConnectionID;
            InputNodeID = inputNode;
            OutputNodeID = outputNode;
            Weight = weight;
            IsEnabled = true;
            IsRecurrent = false;
            PerturbChance = .9;
            MaxWeightPerturbation = 0.1;
            CompatibilityThreshold = .5;
        }

        public void Mutate()
        {
            Random random = NEAT.Random;
            if (random.NextDouble() < PerturbChance)
            {
                Weight += (random.NextDouble() * 2 - 1) * MaxWeightPerturbation;
            } else {
                Weight = random.NextDouble() * 2 - 1;
            }
        }

        public double GetCompatibilityDistance(Connection other)
        {
            double weightDifference = Math.Abs(Weight - other.Weight);
            return (weightDifference * CompatibilityThreshold) + ((IsRecurrent != other.IsRecurrent) ? 1 : 0);
        }

        public void Disable()
        {
            IsEnabled = false;
        }

        public Connection Clone()
        {
            Connection clone = new Connection(InputNodeID, OutputNodeID, Weight, ID);
            clone.IsEnabled = IsEnabled;
            return clone;
        }

    }


}

