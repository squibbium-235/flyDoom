using FlyDoom.Neural.Models;
using FlyDoom.Neural.Simulation;

namespace FlyDoom.Tests;

public sealed class LifNeuronModelTests
{
    [Fact]
    public void State_StartsAtRequestedMembranePotential()
    {
        var state =
            new NeuronStateTable(
                neuronCount: 2,
                initialMembranePotentialMv: -60f);

        Assert.Equal(
            -60f,
            state.GetMembranePotentialMv(0));

        Assert.Equal(
            -60f,
            state.GetMembranePotentialMv(1));
    }

    [Fact]
    public void ExternalDrive_DepolarisesNeuron()
    {
        var parameters =
            LifNeuronParameters.Default;

        var state =
            new NeuronStateTable(
                1,
                parameters.RestingPotentialMv);

        var model =
            new LifNeuronModel(
                parameters);

        state.AddExternalDriveMv(
            0,
            20f);

        model.Step(
            state,
            1f);

        Assert.True(
            state.GetMembranePotentialMv(0) >
            parameters.RestingPotentialMv);
    }

    [Fact]
    public void ExternalDrive_IsConsumedAfterOneStep()
    {
        var parameters =
            LifNeuronParameters.Default;

        var state =
            new NeuronStateTable(
                1,
                parameters.RestingPotentialMv);

        var model =
            new LifNeuronModel(
                parameters);

        state.AddExternalDriveMv(
            0,
            20f);

        model.Step(
            state,
            1f);

        Assert.Equal(
            0f,
            state.GetExternalDriveMv(0));
    }

    [Fact]
    public void SynapticInput_DecaysInsteadOfDisappearing()
    {
        var parameters =
            LifNeuronParameters.Default;

        var state =
            new NeuronStateTable(
                1,
                parameters.RestingPotentialMv);

        var model =
            new LifNeuronModel(
                parameters);

        state.AddSynapticInputMv(
            0,
            20f);

        model.Step(
            state,
            1f);

        var expected =
            20f *
            MathF.Exp(
                -1f /
                parameters.SynapticTimeConstantMs);

        Assert.Equal(
            expected,
            state.GetSynapticInputMv(0),
            precision: 4);
    }

    [Fact]
    public void Step_FiresWhenThresholdIsReached()
    {
        var parameters =
            CreateFastTestParameters();

        var state =
            new NeuronStateTable(
                1,
                parameters.RestingPotentialMv);

        var model =
            new LifNeuronModel(
                parameters);

        state.AddExternalDriveMv(
            0,
            20f);

        model.Step(
            state,
            1f);

        Assert.True(
            state.DidFire(0));

        Assert.Equal(
            parameters.ResetPotentialMv,
            state.GetMembranePotentialMv(0));

        Assert.Equal(
            parameters.RefractoryPeriodMs,
            state.GetRefractoryRemainingMs(0));
    }

    [Fact]
    public void RefractoryNeuron_CannotFireImmediatelyAgain()
    {
        var parameters =
            CreateFastTestParameters();

        var state =
            new NeuronStateTable(
                1,
                parameters.RestingPotentialMv);

        var model =
            new LifNeuronModel(
                parameters);

        state.AddExternalDriveMv(
            0,
            20f);

        model.Step(
            state,
            1f);

        Assert.True(
            state.DidFire(0));

        state.AddExternalDriveMv(
            0,
            100f);

        model.Step(
            state,
            1f);

        Assert.False(
            state.DidFire(0));

        Assert.Equal(
            parameters.ResetPotentialMv,
            state.GetMembranePotentialMv(0));
    }

    [Fact]
    public void MembranePotential_LeaksTowardRest()
    {
        var parameters =
            LifNeuronParameters.Default;

        var state =
            new NeuronStateTable(
                1,
                initialMembranePotentialMv: -50f);

        var model =
            new LifNeuronModel(
                parameters);

        model.Step(
            state,
            1f);

        var newPotential =
            state.GetMembranePotentialMv(0);

        Assert.True(
            newPotential < -50f);

        Assert.True(
            newPotential >
            parameters.RestingPotentialMv);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void State_RejectsInvalidNeuronIndex(
        int neuronIndex)
    {
        var state =
            new NeuronStateTable(
                1,
                -60f);

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                state.GetMembranePotentialMv(
                    neuronIndex));
    }

    private static LifNeuronParameters
        CreateFastTestParameters()
    {
        return new LifNeuronParameters(
            restingPotentialMv: -60f,
            resetPotentialMv: -65f,
            thresholdPotentialMv: -50f,
            membraneTimeConstantMs: 1f,
            synapticTimeConstantMs: 5f,
            refractoryPeriodMs: 2f);
    }
}