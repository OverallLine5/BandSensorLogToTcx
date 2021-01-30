/* 
 * This file is part of the BandSensorLogToTcx distribution (https://github.com/OverallLine5/BandSensorLogToTcx).
 * Copyright (c) 2021 Marcus Lösel (OverallLine5).
 * 
 * This program is free software: you can redistribute it and/or modify  
 * it under the terms of the GNU General Public License as published by  
 * the Free Software Foundation, version 3.
 *
 * This program is distributed in the hope that it will be useful, but 
 * WITHOUT ANY WARRANTY; without even the implied warranty of 
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU 
 * General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License 
 * along with this program. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BandSensorLogToTcx
{
    //=========================================================================================================
    class Program
    //=========================================================================================================
    {
        //------------------------------------------------------------------------------------------------------
        static void Main( string[] args )
        //------------------------------------------------------------------------------------------------------
        {
            string strSensorLogPath = ".";
            string strWorkoutPath = ".";

            Console.WriteLine( System.Reflection.Assembly.GetExecutingAssembly().GetName().Name + " (C) 2021 Marcus Lösel" );
            Console.WriteLine();

            if( args.Length <= 1 )
            {
                Console.WriteLine( "Usage: " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Name + " <path containing sensor log files> <path to store exported workouts>" );
                return;
            }
            else
            {
                strSensorLogPath = args[0];
                strWorkoutPath = args[1];

                if( !Directory.Exists( strSensorLogPath ) )
                {
                    Console.WriteLine( strSensorLogPath + " does not exist." );
                    return;
                }
                if( !Directory.Exists( strWorkoutPath ) )
                {
                    Console.WriteLine( strWorkoutPath + " does not exist." );
                    return;
                }
            }

            SensorLog sensorLog = new SensorLog();
            try
            {
                Dictionary<DateTime, string> dictFiles = new Dictionary<DateTime, string>();

                var dataFiles = Directory.EnumerateFiles( strSensorLogPath, "*.*", SearchOption.TopDirectoryOnly );
                string strFileName;
                DateTime dtFile = System.DateTime.MinValue;

                foreach( string currentFile in dataFiles )
                {
                    using( var fileStream = new FileStream( currentFile, FileMode.Open, FileAccess.Read, FileShare.Read ) )
                    {
                        if( SensorLog.IsSensorLog( fileStream, out dtFile ) )
                            dictFiles.Add( dtFile, currentFile );
                    }
                }

                var dateTimesAscending = dictFiles.Keys.OrderBy( d => d );
                foreach( var dtCurrent in dateTimesAscending )
                {
                    strFileName = dictFiles[dtCurrent];

                    using( var fileStream = new FileStream( strFileName, FileMode.Open, FileAccess.Read, FileShare.Read ) )
                    {
                        sensorLog.Read( fileStream );
                    }
                }
                if( dictFiles.Count == 0 )
                {
                    Console.WriteLine( strSensorLogPath + " does not contain any sensor log files." );
                    return;
                }
            }
            catch( Exception ex )
            {
                Console.WriteLine( "Unable to read sensor log files: " + ex.Message );
                return;
            }

            try
            {
                // create workouts
                if( sensorLog.Sequences.Count > 0 )
                {
                    var workouts = sensorLog.CreateWorkouts();
                    var iCount = 0;
                    if( workouts.Count > 0 )
                    {
                        foreach( var workout in workouts )
                        {
                            if( workout.TrackPoints.Count > 0 )
                            {
                                var pathTcx = Path.Combine( strWorkoutPath, workout.Filename );
                                if( !File.Exists( pathTcx ) )
                                {
                                    using( StreamWriter outputFile = new StreamWriter( pathTcx ) )
                                    {
                                        outputFile.Write( workout.TCXBuffer );
                                        Console.WriteLine( "Exporting " + workout.Filename );
                                        iCount++;
                                    }
                                }
                                else
                                    Console.WriteLine( workout.Filename + " already exists." );
                            }
                        }
                        Console.WriteLine();
                        if( iCount > 0 )
                            Console.WriteLine( "Successfully exported " + iCount + " workouts." );
                        else
                            Console.WriteLine( "No workout was exported." );
                    }
                }
            }
            catch( Exception ex )
            {
                Console.WriteLine( "Unable to write workout files: " + ex.Message );
                return;
            }
        }
    }
}
