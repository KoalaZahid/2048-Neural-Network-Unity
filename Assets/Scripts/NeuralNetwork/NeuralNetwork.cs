using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NeuralNetwork
{

    public readonly Layer[] layers;
    public readonly int[] layerSizes;

    public static int startID = 1;

    public ICost cost;
    System.Random rng;

    public NeuralNetwork(params int[] layerSizes)
    {
        this.layerSizes = layerSizes;
        rng = new System.Random();

        layers = new Layer[layerSizes.Length - 1];
        for (int i = 0; i < layers.Length; i++)
        {
            layers[i] = new Layer(layerSizes[i], layerSizes[i+1], rng);
        }
    }

    public (int predictedClass, double[] outputs) Run(double[] inputs) 
    {
        var outputs = CalculateOutputs(inputs);
        int predictedClass =  MaxValueIndex(outputs);
        return (predictedClass, outputs);
    }

    public double[] CalculateOutputs(double[] inputs)
    {
        foreach (Layer layer in layers)
        {
            inputs = layer.CalculateOutputs(inputs);
        }
        return inputs;
    }

    public static void SaveToFile(NeuralNetworkSave networkSave, string path)
    {
        using (var writer = new System.IO.StreamWriter(path))
        {
            writer.Write(networkSave.SerializeNetwork());
        }
    }

    public static NeuralNetworkSave LoadFromFile(string path)
    {
        using (var reader = new System.IO.StreamReader(path))
        {
            string data = reader.ReadToEnd();
            return UnityEngine.JsonUtility.FromJson<NeuralNetworkSave>(data);
        }
    }

    public NeuralNetworkSave SaveNetwork()
    {
        NeuralNetworkSave Return = new NeuralNetworkSave();
        Return.layerSizes = this.layerSizes;
        Return.connections = new ConnectionSaveData[this.layers.Length];
        // Return.costFunctionType = (Cost.CostType)this.cost.CostFunctionType();

        for (int i = 0; i < this.layers.Length; i++)
        {
            Return.connections[i] = new ConnectionSaveData();
            Return.connections[i].weights = this.layers[i].weights;
            Return.connections[i].biases = this.layers[i].biases;
            Return.connections[i].activationType = this.layers[i].activation.GetActivationType();
        }

        return Return;
    }

    public NeuralNetwork LoadNewNetwork(NeuralNetworkSave network)
    {
        NeuralNetwork newNet = new NeuralNetwork(network.layerSizes);
        for (int i = 0; i < newNet.layers.Length; i++)
        {
            ConnectionSaveData loadedConnections = network.connections[i];
            System.Array.Copy(loadedConnections.weights,newNet.layers[i].weights, loadedConnections.weights.Length);
            System.Array.Copy(loadedConnections.biases,newNet.layers[i].biases, loadedConnections.biases.Length);
            newNet.layers[i].activation = Activation.GetActivationFromType(loadedConnections.activationType);
        }
        // newNet.SetCostFunction(Cost.GetCostFromType( (Cost.CostType)network.costFunctionType ));
        return newNet;
    }

    public static void doubleArrayPrint(double[] input)
    {
        var node = 0;
        foreach (double val in input) {
            node++;
            Debug.Log("Node "+node.ToString()+": " + val.ToString());
        }
    }

    public void Mutate(float amount)
    {
        for (int i = 0; i < layers.Length; i++)
        {
            var layer = layers[i];
            layer.Mutate(amount);
        }
    }

    public int MaxValueIndex(double[] values)
	{
		double maxValue = double.MinValue;
		int index = 0;
		for (int i = 0; i < values.Length; i++)
		{
			if (values[i] > maxValue)
			{
				maxValue = values[i];
				index = i;
			}
		}

		return index;
	}

    public void SetCostFunction(ICost costFunction)
    {
        this.cost = costFunction;
    }
    public void SetActivationFunction(IActivation activation, IActivation outputActivations)
    {
        for (int i = 0; i < layers.Length -1; i++)
        {
            layers[i].SetActivationFunction(activation);
        }
        layers[layers.Length - 1].SetActivationFunction(outputActivations);
    }
    public void SetActivationFunction(IActivation activation)
    {
        SetActivationFunction(activation,activation);
    }

}

[System.Serializable]
public class NeuralNetworkSave
{
    public int[] layerSizes;
    public ConnectionSaveData[] connections;
    public bool unchanging = true;
    public int networkId = NeuralNetwork.startID++;

    public void MergeNetworks(NeuralNetworkSave other)
    {
        if (this.connections.Length != other.connections.Length) Debug.LogWarning("Networks don't have the same layers: NeuralNetworkSave.MergeNetworks()");
        for (int i = 0; i < this.connections.Length; i++)
        {
            (double[] newWeights, double[] newBiases) = Layer.MergeLayers(
                this.connections[i].weights,
                this.connections[i].biases,
                other.connections[i].weights,
                other.connections[i].biases
                );
        }
    }

    public string SerializeNetwork()
    {
        return UnityEngine.JsonUtility.ToJson(this);
    }

    public Cost.CostType costFunctionType;
}

[System.Serializable]
public class ConnectionSaveData
{
    public double[] weights;
    public double[] biases;
    public Activation.ActivationType activationType;

}