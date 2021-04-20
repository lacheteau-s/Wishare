<template>
	<div>
		<h1>Signup</h1>
			<v-row justify="center">
				<v-col cols="4">
					<v-form v-model="isValid">
						<v-text-field
							label="Username"
							v-model="username"
							:rules="rules.username"
							required/>
						<v-text-field
							class="mt-2"
							label="Email"
							v-model="email"
							:rules="rules.email"
							required/>
						<v-text-field
							class="mt-2"
							:type="showPassword ? 'text' : 'password'"
							:append-icon="showPassword ? 'mdi-eye' : 'mdi-eye-off'"
							@click:append="showPassword = !showPassword"
							label="Password"
							v-model="password"
							:rules="rules.password"
							required/>
						<v-btn
							class="mt-4"
							color="primary"
							block>Create account</v-btn>
					</v-form>
				</v-col>
			</v-row>
	</div>
</template>

<script lang="ts">
import { Component, Vue } from "vue-property-decorator"

@Component
export default class Signup extends Vue {

	isValid = false;
	
	username = "";
	email = "";
	password = "";
	showPassword = false;

	rules = {
		username: [
			(v: string) => !!v || "Username is required",
			(v: string) => /^[a-zA-Z0-9]+$/.test(v) || "Username may only contain alphanumeric characters",
			(v: string) => (v.length >= 3 && v.length <= 16) || "Username must be between 3 and 16 characters long"
		],
		email: [
			(v: string) => !!v || "Email is required",
			(v: string) => /^[a-zA-Z0-9.!#$%&'*+\/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$/.test(v) || "Please enter a valid email"
		],
		password: [
			(v: string) => !!v || "Password is required",
			(v: string) => (v.length >= 8 && v.length <= 64) || "Password must be between 8 and 64 characters long"
		]
	};
}
</script>