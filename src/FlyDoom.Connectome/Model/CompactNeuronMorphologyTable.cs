namespace FlyDoom.Connectome.Model;

/// <summary>
/// Stores basic morphological measurements for neurons.
/// </summary>
public sealed class CompactNeuronMorphologyTable
{
    private readonly double[] _lengthNm;
    private readonly double[] _areaNm;
    private readonly double[] _sizeNm;

    public int Count => _lengthNm.Length;

    public CompactNeuronMorphologyTable(
        double[] lengthNm,
        double[] areaNm,
        double[] sizeNm)
    {
        ArgumentNullException.ThrowIfNull(lengthNm);
        ArgumentNullException.ThrowIfNull(areaNm);
        ArgumentNullException.ThrowIfNull(sizeNm);

        if (areaNm.Length != lengthNm.Length ||
            sizeNm.Length != lengthNm.Length)
        {
            throw new ArgumentException(
                "All neuron morphology arrays must have matching lengths.");
        }

        _lengthNm = lengthNm;
        _areaNm = areaNm;
        _sizeNm = sizeNm;
    }

    public bool HasStatistics(int neuronIndex)
    {
        ValidateNeuronIndex(neuronIndex);

        return !double.IsNaN(_lengthNm[neuronIndex]) ||
               !double.IsNaN(_areaNm[neuronIndex]) ||
               !double.IsNaN(_sizeNm[neuronIndex]);
    }

    public double GetLengthNm(int neuronIndex)
    {
        ValidateNeuronIndex(neuronIndex);
        return _lengthNm[neuronIndex];
    }

    public double GetAreaNm(int neuronIndex)
    {
        ValidateNeuronIndex(neuronIndex);
        return _areaNm[neuronIndex];
    }

    public double GetSizeNm(int neuronIndex)
    {
        ValidateNeuronIndex(neuronIndex);
        return _sizeNm[neuronIndex];
    }

    private void ValidateNeuronIndex(int neuronIndex)
    {
        if ((uint)neuronIndex >= (uint)Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(neuronIndex));
        }
    }
}