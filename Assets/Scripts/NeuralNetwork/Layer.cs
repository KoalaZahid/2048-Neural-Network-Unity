using static System.Math;
public class Layer
{
    public IActivation activation;

    public readonly int num_inputNeurons;
    public readonly int num_outputNeurons;

    public double[] weights;
    public double[] biases;

    private System.Random rng = new System.Random();

    public Layer(int num_inputNeurons, int num_outputNeurons, System.Random rng) {
        this.num_inputNeurons = num_inputNeurons;
        this.num_outputNeurons = num_outputNeurons;

        weights = new double[num_inputNeurons * num_outputNeurons];
        biases = new double[num_outputNeurons];

        activation = new Activation.Sigmoid();

        InitializeRandomWeights(rng);
    }

    public double[] CalculateOutputs(double[] inputs)
    {
        double[] weightedInputs = new double[num_outputNeurons];

        for (int nodeOut = 0; nodeOut < num_outputNeurons; nodeOut++)
        {
            double weightedInput = biases[nodeOut];
            for (int nodeIn = 0; nodeIn < num_inputNeurons; nodeIn++)
            {
                weightedInput += inputs[nodeIn] * GetWeight(nodeIn, nodeOut);
            }
            weightedInputs[nodeOut] = weightedInput;
        }

        // Do the activation
        double[] activations = new double[num_outputNeurons];
        for (int outputNode = 0; outputNode < num_outputNeurons; outputNode++)
        {
            activations[outputNode] = activation.Activate(weightedInputs,outputNode);
        }

        return activations;
    }

    public double GetWeight(int nodeIn, int nodeOut)
    {
        int index = nodeOut * num_inputNeurons + nodeIn;
        return weights[index];
    }

    public double[] GetWeights()
    {
        return weights;
    }
    public double[] GetBiases()
    {
        return biases;
    }
    public void SetWeights(double[] newWeights)
    {
        weights = newWeights;
    }
    public void SetBiases(double[] newBiases)
    {
        biases = newBiases;
    }

    // Sets up the weights for the layer randomly
    public void InitializeRandomWeights(System.Random rng)
	{
		for (int i = 0; i < weights.Length; i++)
		{
			weights[i] =   RandomInNormalDistribution(rng, 0, 1) / Sqrt(num_inputNeurons); //rng.NextDouble() * 2 - 1;
		}

		double RandomInNormalDistribution(System.Random rng, double mean, double standardDeviation)
		{
			double x1 = 1 - rng.NextDouble();
			double x2 = 1 - rng.NextDouble();

			double y1 = Sqrt(-2.0 * Log(x1)) * Cos(2.0 * PI * x2);
			return y1 * standardDeviation + mean;
		}
	}

    public void Mutate(float amount)
    {
        System.Random rnd = new System.Random();
        for (int i = 0; i < weights.Length; i++)
        {
            var random = (rnd.NextDouble() < .5)? -1 : 1;
            weights[i] += amount * random;
            biases[i] += rnd.NextDouble() * random;
        }
    }

    public static (double[] newWeights, double[] newBiases) MergeLayers(double[] thisWeights, double[] thisBiases, double[] otherWeights, double[] otherBiases)
    {
        if (thisWeights.Length != otherWeights.Length) UnityEngine.Debug.LogWarning("Weights aren't the same size"); // this should never run
        if (thisBiases.Length != otherBiases.Length) UnityEngine.Debug.LogWarning("Biases aren't the same size"); // this should never run
        double[] newWeights = new double[thisWeights.Length];
        double[] newBiases = new double[thisBiases.Length];
        for (int layerIndex = 0; layerIndex < thisWeights.Length; layerIndex++)
        {
            double Value = (otherWeights[layerIndex]+thisWeights[layerIndex])/2;
            newWeights[layerIndex] = Value;
        }
        for (int layerIndex = 0; layerIndex < thisBiases.Length; layerIndex++)
        {
            double Value = (otherBiases[layerIndex]+thisBiases[layerIndex])/2;
            newBiases[layerIndex] = Value;
        }
        return (newWeights,newBiases);
    }

    public void SetActivationFunction(IActivation activation)
    {
        this.activation = activation;
    }

    public override string ToString()
    {
        string output = "[";
        foreach (double weight in weights)
        {
            output += weight.ToString() + ", ";
        }
        output += "]";
        return output;
    }
}
