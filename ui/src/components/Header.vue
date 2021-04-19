<template>
	<v-app-bar
		app
		color="primary">
		Wishare

		<v-spacer />

		<v-btn 
			rounded
			text
			class="mx-1"
			@click="onClick">Sign in</v-btn>
		<v-btn
			rounded
			class="mx-1"
			@click="onClick">Sign up</v-btn>
	</v-app-bar>
</template>

<script lang="ts">

import { Vue, Component } from 'vue-property-decorator'

@Component
export default class Header extends Vue {
	onClick() {
		console.log("test");
	}
}

</script>