// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using Amazon.CDK;
using Amazon.CDK.AWS.AppRunner;
using Amazon.CDK.AWS.IAM;
using AWS.Deploy.Recipes.CDK.Common;
using hello_apprunner.Deployment.Configurations;
using Amazon.CDK.AWS.ECR;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.EC2;

using CfnService = Amazon.CDK.AWS.AppRunner.CfnService;
using CfnServiceProps = Amazon.CDK.AWS.AppRunner.CfnServiceProps;
using Constructs;

namespace hello_apprunner.Deployment
{
    public class AppStack : Stack
    {
        private readonly Configuration _configuration;

        internal AppStack(Construct scope, IDeployToolStackProps<Configuration> props)
            : base(scope, props.StackName, props)
        {
            _configuration = props.RecipeProps.Settings;

            // Setup callback for generated construct to provide access to customize CDK properties before creating constructs.
            CDKRecipeCustomizer<Recipe>.CustomizeCDKProps += CustomizeCDKProps;

            // Create custom CDK constructs here that might need to be referenced in the CustomizeCDKProps. For example if
            // creating a DynamoDB table construct and then later using the CDK construct reference in CustomizeCDKProps to
            // pass the table name as an environment variable to the container image.

            // Get default VPC

            var defaultVpc = Vpc.FromLookup(this, "default-vpc", new VpcLookupOptions { IsDefault = true });

            // Create VPC Connector

            var securityGroup = new SecurityGroup(this, "sg-vpc-connector", new SecurityGroupProps
                {
                    AllowAllOutbound = true,
                    Vpc = defaultVpc
                });

            securityGroup.AddIngressRule(
                Peer.AnyIpv4(),
                Port.AllTraffic()
            );

            securityGroup.AddEgressRule(
                Peer.AnyIpv4(),
                Port.AllTraffic()
            );

            var vpcConnector = new CfnVpcConnector(this, "apprunner-vpc-conn",
               new CfnVpcConnectorProps
               {
                   VpcConnectorName = "apprunner-vpc-connector",
                   Subnets = defaultVpc.SelectSubnets(null).SubnetIds,
                   SecurityGroups = new string[] { securityGroup.SecurityGroupId }
               });

            new CfnOutput(this, "apprunner-vpc-connector", new CfnOutputProps { Value = vpcConnector.LogicalId });

            // Create DynamoDB table

            var tableProps = new TableProps
            {
                RemovalPolicy = RemovalPolicy.DESTROY,
                TableName = "OnThisDate",
                PartitionKey = new Amazon.CDK.AWS.DynamoDB.Attribute
                {
                    Name = "Day",       // 12/07
                    Type = AttributeType.STRING
                },
                SortKey = new Amazon.CDK.AWS.DynamoDB.Attribute
                {
                    Name = "Title",     // 1941 Pearl Harbor Attack
                    Type = AttributeType.STRING
                },
                BillingMode = BillingMode.PAY_PER_REQUEST,
            };
            var table = new Table(this, "OnThisDate", tableProps);

            new CfnOutput(this, "DynamoDB-OnThisDate", new CfnOutputProps{ Value = table.TableName });

            // Create VPC Endpoint for DynamoDB

            var dynamoGatewayEndpoint = defaultVpc.AddGatewayEndpoint("dynamo-gateway-endpoint", new GatewayVpcEndpointOptions 
            {
                Service = GatewayVpcEndpointAwsService.DYNAMODB
            });

            new CfnOutput(this, "dynamo-gateway-endpoint", new CfnOutputProps{ Value = dynamoGatewayEndpoint.VpcEndpointId });

            // Create the recipe defined CDK construct with all of its sub constructs.
            var generatedRecipe = new Recipe(this, props.RecipeProps);

            // Create additional CDK constructs here. The recipe's constructs can be accessed as properties on
            // the generatedRecipe variable.

            // Add IAM policy granting access to DynamoDB table

            var policyDoc = new PolicyDocument(new PolicyDocumentProps()
            {
                Statements = new PolicyStatement[] {
                    new PolicyStatement(new PolicyStatementProps()
                    {
                        Effect = Effect.ALLOW,
                        Actions = new string[] { "dynamodb:*" },
                        Resources = new string[] { table.TableArn }
                    })
                }
            });

            var policyDynamoTable = new ManagedPolicy(this, "dynamodb-on-this-date", new ManagedPolicyProps()
            {
                ManagedPolicyName = "dynamodb-policy-on-this-date",
                Document = policyDoc
            });

            // Add policies to instance role.

            generatedRecipe.TaskRole.AddManagedPolicy(ManagedPolicy.FromAwsManagedPolicyName("AmazonDynamoDBFullAccess"));
            generatedRecipe.TaskRole.AddManagedPolicy(ManagedPolicy.FromAwsManagedPolicyName("AWSAppRunnerFullAccess"));
            generatedRecipe.TaskRole.AddManagedPolicy(policyDynamoTable);

            // Associate VPC Connector with App Runner service

            var appRunner = generatedRecipe.AppRunnerService;
            if (generatedRecipe != null && generatedRecipe.AppRunnerService != null)
            {
                var egressConfig = new CfnService.EgressConfigurationProperty
                {
                    EgressType = "VPC",
                    VpcConnectorArn = vpcConnector.AttrVpcConnectorArn
                };
                var network = new CfnService.NetworkConfigurationProperty
                {
                    EgressConfiguration = egressConfig
                };
                generatedRecipe.AppRunnerService.NetworkConfiguration = network;
            }

        }

        /// <summary>
        /// This method can be used to customize the properties for CDK constructs before creating the constructs.
        ///
        /// The pattern used in this method is to check to evnt.ResourceLogicalName to see if the CDK construct about to be created is one
        /// you want to customize. If so cast the evnt.Props object to the CDK properties object and make the appropriate settings.
        /// </summary>
        /// <param name="evnt"></param>
        private void CustomizeCDKProps(CustomizePropsEventArgs<Recipe> evnt)
        {
            // Example of how to customize the container image definition to include environment variables to the running applications.
            // 
            //if (string.Equals(evnt.ResourceLogicalName, nameof(evnt.Construct.AppRunnerService)))
            //{
            //    if (evnt.Props is CfnServiceProps props)
            //    {
            //        Console.WriteLine("Customizing AppRunner Service");
            //    }
            //}
        }
    }
}
