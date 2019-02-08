//$ Copyright 2016, Code Respawn Technologies Pvt Ltd - All Rights Reserved $//

using UnityEngine;
using System.Collections;


namespace DungeonArchitect.Utils
{
    /// <summary>
    /// Utility function to extract and put data into a Matrix4x4 object
    /// </summary>
	public class Matrix {
		/// <summary>
		/// Extract translation from transform matrix.
		/// </summary>
		/// <param name="matrix">Transform matrix. This parameter is passed by reference
		/// to improve performance; no changes will be made to it.</param>
		/// <returns>
		/// Translation offset.
		/// </returns>
		public static Vector3 GetTranslation(ref Matrix4x4 matrix) {
			Vector3 translate;
			translate.x = matrix.m03;
			translate.y = matrix.m13;
			translate.z = matrix.m23;
			return translate;
		}
		
        /// <summary>
        /// Sets the translation of the matrix object
        /// </summary>
        /// <param name="matrix">The matrix to set the translation on</param>
        /// <param name="translate">The translation to apply on the matrix</param>
		public static void SetTranslation(ref Matrix4x4 matrix, Vector3 translate) {
			matrix.m03 = translate.x;
			matrix.m13 = translate.y;
			matrix.m23 = translate.z;
		}

        
        /// <summary>
        /// Sets the transform of a matrix
        /// </summary>
        /// <param name="transform">The matrix object to apply the transformation on</param>
        /// <param name="position">The position to set</param>
        /// <param name="rotation">The rotation to set</param>
        /// <param name="scale">The scale ot set</param>
		public static void SetTransform(out Matrix4x4 transform, Vector3 position, Quaternion rotation, Vector3 scale) {
			transform = Matrix4x4.TRS(position, rotation, scale);
		}
		
		/// <summary>
		/// Extract rotation quaternion from transform matrix.
		/// </summary>
		/// <param name="matrix">Transform matrix. This parameter is passed by reference
		/// to improve performance; no changes will be made to it.</param>
		/// <returns>
		/// Quaternion representation of rotation transform.
		/// </returns>
		public static Quaternion GetRotation(ref Matrix4x4 matrix) {
			Vector3 forward;
			forward.x = matrix.m02;
			forward.y = matrix.m12;
			forward.z = matrix.m22;
			
			Vector3 upwards;
			upwards.x = matrix.m01;
			upwards.y = matrix.m11;
			upwards.z = matrix.m21;

            if (forward == Vector3.zero)
            {
                return Quaternion.identity;
            }
			return Quaternion.LookRotation(forward, upwards);
		}
		
		/// <summary>
		/// Extract scale from transform matrix.
		/// </summary>
		/// <param name="matrix">Transform matrix. This parameter is passed by reference
		/// to improve performance; no changes will be made to it.</param>
		/// <returns>
		/// Scale vector.
		/// </returns>
		public static Vector3 GetScale(ref Matrix4x4 matrix) {
			Vector3 scale;
			scale.x = new Vector4(matrix.m00, matrix.m10, matrix.m20, matrix.m30).magnitude;
			scale.y = new Vector4(matrix.m01, matrix.m11, matrix.m21, matrix.m31).magnitude;
			scale.z = new Vector4(matrix.m02, matrix.m12, matrix.m22, matrix.m32).magnitude;
			return scale;
		}
		
		/// <summary>
		/// Extract position, rotation and scale from TRS matrix.
		/// </summary>
		/// <param name="matrix">Transform matrix. This parameter is passed by reference
		/// to improve performance; no changes will be made to it.</param>
		/// <param name="localPosition">Output position.</param>
		/// <param name="localRotation">Output rotation.</param>
		/// <param name="localScale">Output scale.</param>
		public static void DecomposeMatrix(ref Matrix4x4 matrix, out Vector3 localPosition, out Quaternion localRotation, out Vector3 localScale) {
			localPosition = GetTranslation(ref matrix);
			localRotation = GetRotation(ref matrix);
			localScale = GetScale(ref matrix);
		}
		
		/// <summary>
		/// Set transform component from TRS matrix.
		/// </summary>
		/// <param name="transform">Transform component.</param>
		/// <param name="matrix">Transform matrix. This parameter is passed by reference
		/// to improve performance; no changes will be made to it.</param>
		public static void SetTransformFromMatrix(Transform transform, ref Matrix4x4 matrix) {
			transform.localPosition = GetTranslation(ref matrix);
			transform.localRotation = GetRotation(ref matrix);
			transform.localScale = GetScale(ref matrix);
		}
		
		
		// EXTRAS!
		
		/// <summary>
		/// Identity quaternion.
		/// </summary>
		/// <remarks>
		/// <para>It is faster to access this variation than <c>Quaternion.identity</c>.</para>
		/// </remarks>
		public static readonly Quaternion IdentityQuaternion = Quaternion.identity;
		/// <summary>
		/// Identity matrix.
		/// </summary>
		/// <remarks>
		/// <para>It is faster to access this variation than <c>Matrix4x4.identity</c>.</para>
		/// </remarks>
		//public static readonly Matrix4x4 IdentityMatrix = Matrix4x4.identity;

		public static Matrix4x4 Identity() {
			return Copy (Matrix4x4.identity);
		}

		/// <summary>
		/// Get translation matrix.
		/// </summary>
		/// <param name="offset">Translation offset.</param>
		/// <returns>
		/// The translation transform matrix.
		/// </returns>
		public static Matrix4x4 TranslationMatrix(Vector3 offset) {
			Matrix4x4 matrix = Identity();
			matrix.m03 = offset.x;
			matrix.m13 = offset.y;
			matrix.m23 = offset.z;
			return matrix;
		}

        /// <summary>
        /// Copies the matrix object
        /// </summary>
        /// <param name="In">The matrix object to copy</param>
        /// <returns>The copied object</returns>
		public static Matrix4x4 Copy(Matrix4x4 In) {
			return In * Matrix4x4.identity;
		}

        /// <summary>
        /// Creates a Matrix4x4 object from the game object's transform
        /// </summary>
        /// <param name="t">The game object's transform</param>
        /// <returns>The resulting matrix</returns>
		public static Matrix4x4 FromGameTransform(Transform t) {
			return Matrix4x4.TRS(t.position, t.rotation, t.localScale);
		}
	}
}
