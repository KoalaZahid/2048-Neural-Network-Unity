public interface IActivation
{
    double Activate(double input);
    double Activate(double[] inputs, int index);
    double Derivative(double[] inputs, int index);
    Activation.ActivationType GetActivationType();
}
