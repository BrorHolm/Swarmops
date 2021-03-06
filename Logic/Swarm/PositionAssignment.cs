﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NBitcoin;
using Swarmops.Basic.Types.Swarm;
using Swarmops.Common.Enums;
using Swarmops.Database;
using Swarmops.Logic.Structure;

namespace Swarmops.Logic.Swarm
{
    public class PositionAssignment: BasicPositionAssignment
    {
        private PositionAssignment() : base (null)
        {
            // do not call private null ctor
        }

        private PositionAssignment (BasicPositionAssignment basic) : base (basic)
        {
            // private ctor
        }

        public static PositionAssignment FromBasic (BasicPositionAssignment basic)
        {
            return new PositionAssignment (basic); // calls private ctor
        }

        public static PositionAssignment FromIdentity (int positionAssignmentId)
        {
            return FromBasic (SwarmDb.GetDatabaseForReading().GetPositionAssignment (positionAssignmentId));
        }

        public static PositionAssignment FromIdentityAggressive (int positionAssignmentId)
        {
            return FromBasic(SwarmDb.GetDatabaseForWriting().GetPositionAssignment(positionAssignmentId)); // "ForWriting" is intentional - avoids race conditions in Create()
        }


        public Person Person
        {
            get { return Person.FromIdentity (base.PersonId); }
        }

        public Position Position
        {
            get { return Position.FromIdentity (base.PositionId); }
        }

        public static PositionAssignment Create (Organization organization, Geography geography, Position position,
            Person person, Person createdByPerson, Position createdByPosition, DateTime? expiresDateTimeUtc,
            string assignmentNotes)
        {
            int organizationId = 0;

            if (position.PositionLevel != PositionLevel.OrganizationwideDefault &&
                position.PositionLevel != PositionLevel.Systemwide)
            {
                organizationId = organization.Identity; // can't be null in these cases
            }
            else
            {
                if (organization != null)
                {
                    // MUST be null in these cases

                    throw new ArgumentException ("Organization cannot be defined when position is systemwide");
                }
            }

            int geographyId = 0;

            if (position.PositionLevel == PositionLevel.Geography)
            {
                geographyId = geography.Identity;
            }
            else
            {
                if (geography != null)
                {
                    throw new ArgumentException ("Geography cannot be defined when position is global");
                }
            }

            int createdByPersonId = createdByPerson == null ? 0 : createdByPerson.Identity;
            int createdByPositionId = createdByPosition == null ? 0 : createdByPosition.Identity;

            // TODO: Verify constraints of max/min counts for position

            int positionAssignmentId = SwarmDb.GetDatabaseForWriting()
                .CreatePositionAssignment (organizationId, geographyId, position.Identity, person.Identity,
                    createdByPersonId, createdByPositionId,
                    expiresDateTimeUtc == null ? DateTime.MaxValue : (DateTime) expiresDateTimeUtc, assignmentNotes);

            return FromIdentityAggressive (positionAssignmentId);
        }

    }
}
