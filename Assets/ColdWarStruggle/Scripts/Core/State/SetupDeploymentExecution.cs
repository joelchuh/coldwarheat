namespace ColdWarStruggle.Core
{
    public sealed class SetupDeploymentExecution
    {
        public string Id { get; }
        public SetupDeploymentDefinition Definition { get; }
        public int StageIndex { get; }
        public bool Complete=>StageIndex==Definition.Stages.Count;
        public PlayerSide? Owner=>Complete?(PlayerSide?)null:Definition.Stages[StageIndex].Side;
        public string DecisionId=>Complete?null:Id+":deploy:"+StageIndex.ToString(System.Globalization.CultureInfo.InvariantCulture);
        public long? DecisionSourceVersion { get; }
        internal SetupDeploymentExecution(string id,SetupDeploymentDefinition definition,int stageIndex,long version)
        { Id=id;Definition=definition;StageIndex=stageIndex;DecisionSourceVersion=Complete?(long?)null:version; }
    }
}
