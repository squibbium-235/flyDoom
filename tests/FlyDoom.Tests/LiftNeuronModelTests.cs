using FlyDoom.Neural.Models;
using FlyDoom.Neural.Simulation;

namespace FlyDoom.Tests;

/// <summary>
/// Tests basic leaky integrate-and-fire neuron dynamics.
/// </summary>
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
    public void Step_DepolarisesNeuronWhenDriveIsPositive()
    {
        var parameters =
            LifNeuronParameters.Default;

        var state =
            new NeuronStateTable(
                1,
                parameters.RestingPotentialMv);

        var model =
            new LifNeuronModel(parameters);

        state.AddSynapticDriveMv(
            0,
            20f);

        model.Step(
            state,
            timeStepMs: 1f);

        Assert.True(
            state.GetMembranePotentialMv(0) >
            parameters.RestingPotentialMv);
    }

    [Fact]
    public void Step_ConsumesSynapticDrive()
    {
        var parameters =
            LifNeuronParameters.Default;

        var state =
            new NeuronStateTable(
                1,
                parameters.RestingPotentialMv);

        var model =
            new LifNeuronModel(parameters);

        state.AddSynapticDriveMv(
            0,
            20f);

        model.Step(
            state,
            1f);

        Assert.Equal(
            0f,
            state.GetSynapticDriveMv(0));
    }

    [Fact]
    public void Step_FiresWhenThresholdIsReached()
    {
        var parameters =
            new LifNeuronParameters(
                restingPotentialMv: -60f,
                resetPotentialMv: -65f,
                thresholdPotentialMv: -50f,
                membraneTimeConstantMs: 1f,
                refractoryPeriodMs: 2f);

        var state =
            new NeuronStateTable(
                1,
                parameters.RestingPotentialMv);

        var model =
            new LifNeuronModel(parameters);

        state.AddSynapticDriveMv(
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
    public void Step_RefractoryNeuronCannotFireImmediatelyAgain()
    {
        var parameters =
            new LifNeuronParameters(
                restingPotentialMv: -60f,
                resetPotentialMv: -65f,
                thresholdPotentialMv: -50f,
                membraneTimeConstantMs: 1f,
                refractoryPeriodMs: 2f);

        var state =
            new NeuronStateTable(
                1,
                parameters.RestingPotentialMv);

        var model =
            new LifNeuronModel(parameters);

        state.AddSynapticDriveMv(
            0,
            20f);

        model.Step(
            state,
            1f);

        Assert.True(
            state.DidFire(0));

        state.AddSynapticDriveMv(
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
    public void Step_MembranePotentialLeaksTowardRest()
    {
        var parameters =
            LifNeuronParameters.Default;

        var state =
            new NeuronStateTable(
                1,
                initialMembranePotentialMv: -50f);

        var model =
            new LifNeuronModel(parameters);

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
            () => state.GetMembranePotentialMv(
                neuronIndex));
    }
}